"""Pruebas locales contra un proceso real. Solo inicia/detiene su propio proceso hijo.

Ejecutar con -u, --content-root, --audit-root y --port. En stdin: probe, down, up, quit.
Las bases/archivos deben ser copias; rechaza el uso de la base del proyecto.
"""
import argparse
import concurrent.futures
import hashlib
import http.cookiejar
import html
import json
import os
from pathlib import Path
import re
import shutil
import socket
import sqlite3
import statistics
import subprocess
import sys
import time
import urllib.error
import urllib.parse
import urllib.request


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, request, fp, code, msg, headers, newurl):
        return None


class Session:
    def __init__(self, origin):
        self.origin = origin
        self.cookies = http.cookiejar.CookieJar()
        self.opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(self.cookies), NoRedirect())
        self.token = ""

    def request(self, path, fields=None, body=None, headers=None):
        assert path.startswith("/") and not path.startswith("//")
        if fields is not None:
            body = urllib.parse.urlencode(fields).encode()
        request = urllib.request.Request(self.origin + path, data=body, headers=headers or {})
        start = time.monotonic()
        try:
            response = self.opener.open(request, timeout=25)
        except urllib.error.HTTPError as error:
            response = error
        data = response.read()
        return response.status, data, dict(response.headers), time.monotonic() - start

    def login(self, role):
        status, data, _, _ = self.request("/auth/Auth/Login")
        assert status == 200
        token = html.unescape(re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', data.decode()).group(1))
        status, _, _, _ = self.request("/auth/Auth/Login", {"Email": role + "@si-lab.local", "Password": "Demo123!", "__RequestVerificationToken": token})
        assert status == 302, (role, status)
        status, data, _, _ = self.request("/auth/Auth/Dashboard")
        assert status == 200
        self.token = html.unescape(re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', data.decode()).group(1))
        return self

    def post(self, path, fields):
        return self.request(path, dict(fields, __RequestVerificationToken=self.token))

    def upload(self, path, fields, data):
        boundary = "CompleteAuditBoundary"
        values = dict(fields, __RequestVerificationToken=self.token)
        body = b"".join(("--"+boundary+'\r\nContent-Disposition: form-data; name="'+k+'"\r\n\r\n'+str(v)+'\r\n').encode() for k, v in values.items())
        body += ("--"+boundary+'\r\nContent-Disposition: form-data; name="pdf"; filename="control.pdf"\r\nContent-Type: application/pdf\r\n\r\n').encode()+data+("\r\n--"+boundary+"--\r\n").encode()
        return self.request(path, body=body, headers={"Content-Type": "multipart/form-data; boundary="+boundary})


def tables(path):
    with sqlite3.connect(str(path)) as db:
        names = [r[0] for r in db.execute("select name from sqlite_master where type='table' and name not like 'sqlite_%' order by name")]
        rows = {name: db.execute('select * from "' + name + '" order by rowid').fetchall() for name in names}
    return hashlib.sha256(json.dumps(rows, default=lambda x: x.hex(), sort_keys=True).encode()).hexdigest()


def files(directory):
    return {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in directory.glob("*") if p.is_file()}


class Audit:
    def __init__(self, args):
        self.content = Path(args.content_root).resolve()
        self.root = Path(args.audit_root).resolve()
        self.root.mkdir(exist_ok=True, parents=True)
        self.db = self.root / "local.db"
        self.storage = self.root / "documentos"
        self.origin = "http://localhost:" + str(args.port)
        self.port = args.port
        self.process = None
        self.log = None
        self.report = []
        marker = self.root / "ensayo-local.json"
        if self.root == self.content or self.content in self.root.parents:
            raise ValueError("El ensayo requiere una carpeta independiente del proyecto que se arranca.")
        if self.db.exists() and (not marker.is_file() or json.loads(marker.read_text(encoding="utf-8")).get("purpose") != "disposable-local-audit"):
            raise ValueError("Se rechaza una base existente que no esté identificada como copia del ensayo.")
        if not self.db.exists():
            source = Path(args.seed_db).resolve()
            assert source != self.db
            with sqlite3.connect(str(source)) as a, sqlite3.connect(str(self.db)) as b:
                a.backup(b)
            shutil.copytree(args.seed_files, str(self.storage))
            marker.write_text(json.dumps({"purpose": "disposable-local-audit", "source": str(source)}), encoding="utf-8")
        assert self.db != self.content / "si_lab.db"

    def record(self, name, **data):
        value = dict(test=name, **data)
        self.report.append(value)
        (self.root / "operaciones.json").write_text(json.dumps(self.report, ensure_ascii=False, indent=2), encoding="utf-8")
        print(json.dumps(value, ensure_ascii=False), flush=True)

    def start(self):
        assert self.process is None
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as port_check:
            port_check.settimeout(.25)
            if port_check.connect_ex(("127.0.0.1", self.port)) == 0:
                raise RuntimeError("El puerto ya está en uso; no se probará un proceso ajeno.")
        env = dict(os.environ, ASPNETCORE_ENVIRONMENT="Development", DOTNET_ENVIRONMENT="Development")
        dll = self.content / "bin/Release/net8.0/SEDESLABORATORIO.dll"
        self.log = open(self.root / "servidor.log", "ab")
        self.process = subprocess.Popen([
            "dotnet", str(dll), "--urls", self.origin,
            "--ConnectionStrings:DefaultConnection=Data Source=" + str(self.db) + ";Default Timeout=2",
            "--Storage:Root=" + str(self.storage), "--Auth:SeedDemoUsers=true", "--Auth:SeedDemoData=false",
            "--Logging:LogLevel:Default=Warning", "--Logging:LogLevel:Microsoft.EntityFrameworkCore=Warning"
        ], cwd=str(self.content), env=env, stdout=self.log, stderr=self.log,
            creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0))
        for _ in range(120):
            if self.process.poll() is not None:
                raise RuntimeError("El proceso local no inició. Revisar servidor.log.")
            try:
                if Session(self.origin).request("/publico/Publico")[0] == 200:
                    self.record("arranque", pid=self.process.pid, healthy=True, environment="Development")
                    return
            except (OSError, urllib.error.URLError):
                pass
            time.sleep(.2)
        raise RuntimeError("El servidor local no respondió en el plazo.")

    def stop(self):
        if self.process is not None:
            pid = self.process.pid
            if self.process.poll() is None:
                self.process.kill()
            self.process.wait(timeout=10)
            self.log.close()
            self.process = None
            self.record("detención_del_proceso_hijo", pid=pid, stopped=True)

    def sessions(self):
        return {role: Session(self.origin).login(role) for role in ["propietario", "coordinador", "supervisor", "abogado", "gerente", "administrador"]}

    def upload_case(self):
        owner = Session(self.origin).login("propietario")
        with sqlite3.connect(str(self.db)) as db:
            case = db.execute("select Id,Version from Solicitudes where Estado='borrador' order by Id limit 1").fetchone()
        assert case
        pdf = next(self.storage.glob("*.pdf")).read_bytes()
        return owner, {"id": case[0], "version": case[1], "requisitoId": 0}, pdf

    def storage_failure(self):
        owner, fields, pdf = self.upload_case()
        before, previous = tables(self.db), files(self.storage)
        saved = self.root / "documentos-conservados"
        assert self.storage.parent == self.root and saved.parent == self.root and not saved.exists()
        self.storage.rename(saved)
        self.storage.write_text("Fallo de almacenamiento del ensayo local", encoding="utf-8")
        try:
            status, _, _, _ = owner.upload("/Gestion/Documento", fields, pdf)
        finally:
            assert self.storage.is_file() and self.storage.read_text(encoding="utf-8") == "Fallo de almacenamiento del ensayo local"
            self.storage.unlink()
            saved.rename(self.storage)
        self.record("almacenamiento_no_disponible", status=status, databaseUnchanged=before == tables(self.db), filesUnchanged=previous == files(self.storage))
        assert status == 503 and before == tables(self.db) and previous == files(self.storage)

    def crash_after_file(self):
        owner, fields, pdf = self.upload_case()
        before, previous = tables(self.db), files(self.storage)
        lock = sqlite3.connect(str(self.db), timeout=2)
        lock.execute("BEGIN IMMEDIATE")
        executor = concurrent.futures.ThreadPoolExecutor(max_workers=1)
        request = executor.submit(owner.upload, "/Gestion/Documento", fields, pdf)
        try:
            for _ in range(100):
                if set(files(self.storage)) - set(previous): break
                if request.done(): raise RuntimeError("La petición terminó antes del punto de interrupción.")
                time.sleep(.01)
            else: raise RuntimeError("No se alcanzó la escritura del archivo.")
            self.stop()
        finally:
            lock.rollback()
            lock.close()
            try: request.result(timeout=10)
            except (OSError, urllib.error.URLError): pass
            executor.shutdown(wait=True)
        extra = set(files(self.storage)) - set(previous)
        assert extra and before == tables(self.db)
        self.start()
        recovered = {p.name for p in (self.storage/"recuperacion").glob("*/*") if p.is_file()}
        remaining = set(files(self.storage)) - set(previous)
        assert all((self.storage/name).exists() or name in recovered for name in extra)
        self.record("caida_entre_archivo_y_commit", orphanedBeforeRestart=len(extra), unreferencedAfterRestart=len(remaining), recoverableAfterRestart=len(extra & recovered), committedTablesPreserved=before == tables(self.db), registeredFilesPreserved=all(files(self.storage).get(n)==h for n,h in previous.items()))
        assert not remaining and extra <= recovered and before == tables(self.db)

    def busy_write(self):
        owner, fields, pdf = self.upload_case()
        before, previous = tables(self.db), files(self.storage)
        lock = sqlite3.connect(str(self.db), timeout=2)
        lock.execute("BEGIN IMMEDIATE")
        try:
            status, _, _, _ = owner.upload("/Gestion/Documento", fields, pdf)
        finally:
            lock.rollback()
            lock.close()
        self.record("base_ocupada_durante_guardado", status=status, databaseUnchanged=before == tables(self.db), filesUnchanged=previous == files(self.storage))
        assert status == 409 and before == tables(self.db) and previous == files(self.storage)

    def probe(self):
        clients = self.sessions()
        self.record("restauracion_accesos", roles=list(clients), passed=True)
        with sqlite3.connect(str(self.db)) as db:
            document = db.execute("select Id from Documentos order by Id limit 1").fetchone()[0]
        status, data, _, _ = clients["propietario"].request("/Gestion/Descargar/" + str(document))
        assert status == 200 and data.startswith(b"%PDF-")
        assert clients["gerente"].request("/Gestion/Descargar/" + str(document))[0] == 302
        self.record("restauracion_descarga_privada", pdf=True, unauthorizedDenied=True)
        routes = {"propietario": "/Gestion", "coordinador": "/Gestion", "supervisor": "/Gestion/Agenda", "abogado": "/Gestion", "gerente": "/Metricas/Datos", "administrador": "/Administracion/Usuarios"}
        tasks = [(role, i) for i in range(10) for role in clients]
        def read(pair):
            role, _ = pair
            response = clients[role].request(routes[role])
            return response[0], response[3]
        with concurrent.futures.ThreadPoolExecutor(max_workers=12) as executor:
            responses = list(executor.map(read, tasks))
        assert all(s == 200 for s, _ in responses), [s for s, _ in responses]
        durations = sorted(t for _, t in responses)
        self.record("lectura_concurrente", requests=len(responses), concurrency=12, statuses={str(s): sum(x[0] == s for x in responses) for s, _ in responses}, medianSeconds=round(statistics.median(durations), 3), p95Seconds=round(durations[int(len(durations)*.95)-1], 3), maxSeconds=round(max(durations), 3))
        with sqlite3.connect(str(self.db)) as db:
            lab = db.execute("select Id,Servicios from Laboratorios where Codigo='DEMO-LAB-HABILITADO'").fetchone()
        version = hashlib.sha256(lab[1].encode()).hexdigest().upper()
        def write(n):
            return clients["propietario"].post("/Gestion/Servicios", {"id": lab[0], "version": version, "servicios": "Servicio guardado por petición " + str(n)})[0]
        with concurrent.futures.ThreadPoolExecutor(max_workers=8) as executor:
            statuses = list(executor.map(write, range(16)))
        assert statuses.count(302) == 1 and all(s in (302, 409) for s in statuses), statuses
        self.record("escritura_concurrente", requests=16, concurrency=8, saved=statuses.count(302), staleRejected=statuses.count(409))
        before, oldfiles = tables(self.db), files(self.storage)
        boundary = "AuditInterruptionBoundary"
        values = {"__RequestVerificationToken": clients["propietario"].token, "NombreLaboratorio": "Carga que no debe completarse", "TipoLaboratorio": "Clínico", "Municipio": "Cochabamba", "Latitud": "-17.39", "Longitud": "-66.15"}
        body = b"".join(("--"+boundary+'\r\nContent-Disposition: form-data; name="'+k+'"\r\n\r\n'+v+'\r\n').encode() for k, v in values.items())
        body += ("--"+boundary+'\r\nContent-Disposition: form-data; name="DocumentosPdf"; filename="interrumpido.pdf"\r\nContent-Type: application/pdf\r\n\r\n%PDF-1.7\n').encode()+b"X"*4096
        cookie = "; ".join(c.name+"="+c.value for c in clients["propietario"].cookies)
        header = ("POST /solicitud-registro/SolicitudRegistro/Nueva HTTP/1.1\r\nHost: localhost:"+str(self.port)+"\r\nCookie: "+cookie+"\r\nContent-Type: multipart/form-data; boundary="+boundary+"\r\nContent-Length: 5242880\r\nConnection: close\r\n\r\n").encode()
        with socket.create_connection(("localhost", self.port), timeout=10) as connection:
            connection.sendall(header+body)
        time.sleep(.4)
        assert before == tables(self.db) and oldfiles == files(self.storage)
        self.record("corte_cliente_durante_carga", databaseUnchanged=True, filesUnchanged=True)
        pending = socket.create_connection(("localhost", self.port), timeout=10)
        pending.sendall(header+body)
        self.stop()
        pending.close()
        with sqlite3.connect(str(self.db)) as db:
            assert db.execute("pragma integrity_check").fetchone()[0] == "ok"
            assert not db.execute("pragma foreign_key_check").fetchall()
        assert before == tables(self.db) and oldfiles == files(self.storage)
        self.start()
        assert clients["propietario"].request("/Gestion")[0] == 200
        self.record("reinicio_inesperado_durante_carga", databaseUnchanged=True, filesUnchanged=True, sessionRecovered=True, integrity="ok")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--content-root", required=True)
    parser.add_argument("--audit-root", required=True)
    parser.add_argument("--seed-db", required=True)
    parser.add_argument("--seed-files", required=True)
    parser.add_argument("--port", type=int, default=5271)
    args = parser.parse_args()
    audit = Audit(args)
    try:
        audit.start()
        for line in sys.stdin:
            action = line.strip()
            try:
                if action == "probe": audit.probe()
                elif action == "storage": audit.storage_failure()
                elif action == "crashcommit": audit.crash_after_file()
                elif action == "busy": audit.busy_write()
                elif action == "down": audit.stop()
                elif action == "up": audit.start()
                elif action == "quit": break
                else: print("Comando: probe, down, up, quit", flush=True)
            except Exception as error:
                audit.record("fallo", operation=action, type=type(error).__name__, message=str(error))
    finally:
        audit.stop()
