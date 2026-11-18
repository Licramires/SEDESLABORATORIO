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

## Actualización integrada — 12/10/2026

Se incorpora `abogado` para revisión legal final. Todos los perfiles tienen módulos funcionales. La autenticación rechaza cuentas inactivas, actualiza hashes cuando corresponde y valida un sello de seguridad en cada petición; editar una cuenta revoca su cookie anterior. Los permisos operativos pueden suspenderse por perfil y se comprueban en el servidor. Login tiene límite de 30 intentos por minuto por IP. Los usuarios demo se crean solo en Development con base vacía; en Production el alta inicial utiliza variables `Bootstrap__AdminEmail` y `Bootstrap__AdminPassword`.

## Verificación del 18/10/2026

Se probaron también los permisos de revisión individual, preparación de actas y citaciones. La suite conserva el límite real de login y comprueba que 31 intentos desde el mismo cliente producen 429. Los clientes de TestServer reciben direcciones distintas solo dentro del proyecto de pruebas; no se confía en esa cabecera en la aplicación real. El administrador mantiene acceso a permisos aunque suspenda otras funciones propias.
