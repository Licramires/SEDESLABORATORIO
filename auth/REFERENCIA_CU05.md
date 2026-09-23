# Referencia funcional de CU05

El módulo `auth/` implementa el acceso por rol del sistema SI_Lab.

## Comportamiento

1. El usuario ingresa correo y contraseña.
2. El sistema valida las credenciales contra `Usuario`.
3. La contraseña se verifica mediante un hash PBKDF2 de `PasswordHasher<Usuario>`.
4. Una autenticación correcta crea una sesión mediante cookie segura de ASP.NET Core.
5. La sesión incluye los claims de identidad, correo y rol.
6. El usuario llega al panel inicial y los módulos posteriores pueden proteger acciones con `[Authorize]` o `[Authorize(Roles = "...")]`.
7. El cierre de sesión elimina la cookie.

## Roles contemplados

- `propietario`: gestiona sus solicitudes y documentos.
- `coordinador`: revisa y administra solicitudes.
- `supervisor`: gestiona inspecciones asignadas.
- `gerente`: consulta métricas y reportes.
- `administrador`: administra usuarios, requisitos y permisos.

La reunión de referencia también describe la visibilidad de estas áreas por rol. Las funcionalidades de cada área pertenecen a sus respectivos módulos y no se implementan dentro de `auth/`.

## Referencias entregadas

Los requisitos funcionales se contrastaron con los archivos locales proporcionados en `C:\Users\Usuario\OneDrive\Desktop\Agent-resources`:

- `JUAN y STEVEN.docx` — transcripción de la reunión.
- `Grabación de la reunión de JUAN y STEVEN-20260915_203246.mp4` — video de referencia visual.

El video no se copia al repositorio por su tamaño; se conserva como referencia local para futuras iteraciones.
