# 14. Conclusiones

## 14.1 Conclusión del equipo

El desarrollo de **Bellissimo** permitió integrar de forma práctica el diseño de una base de datos relacional normalizada en **MySQL 8.0** con una aplicación web funcional en **ASP.NET Core Razor Pages (.NET 10)**. A lo largo del proyecto comprobamos que delegar las reglas críticas de integridad, transacciones y cálculos automatizados directamente al SGBD —respaldado por un contrato técnico claro entre capas— garantiza la consistencia y persistencia real de la información operativa de la florería (asistencias, ausencias y pagos semanales), evitando depender únicamente de validaciones en la interfaz.

## 14.2 Conclusión individual — DBA (Jose Antonio Montoya Garcia)

**Decisiones que tomé y por qué**
- Se decidió almacenar en la tabla de `pago` el historial calculado ("foto" de sueldos, descuentos y conceptos al momento de generar y cerrar la semana) para preservar la integridad histórica de los recibos aunque posteriormente cambie el sueldo base de un puesto.
- Se implementaron triggers, procedimientos almacenados y restricciones (`FOREIGN KEY`, `UNIQUE`, `CHECK`) directamente en MySQL 8.0 en lugar de depender solo de la aplicación, junto con un usuario dedicado de mínimo privilegio (`app_floreria`) para proteger el esquema.

**Dificultades**
- Ajustar la sintaxis y el comportamiento estricto al probar sobre MySQL 8.0 real, así como documentar y resolver las incidencias detectadas durante las pruebas de regresión (como el manejo de días sin checada, traslapes de ausencias y bloqueo de cambios en semanas cerradas).
- Formalizar en reglas de negocio verificables procesos cotidianos de la florería que inicialmente se manejaban de manera informal.

**Mi contribución al proyecto**
- Levantamiento de requerimientos del negocio, diseño conceptual y relacional (diagramas, diccionario de datos y normalización), codificación de los scripts SQL (`01_esquema.sql` a `05_usuario_app.sql`), definición del contrato técnico (`CONTRATO_BD.md`) y ejecución de la matriz de pruebas y registro de incidencias.

**Lo que aprendí**
- Aplicar la teoría de normalización (1FN a 3FN y análisis de formas superiores) frente a necesidades reales de auditoría histórica, programar transacciones y triggers robustos en MySQL y coordinar entregas versionadas en equipo mediante Git.

## 14.3 Conclusiones individuales del resto del equipo

### Kenan Rojas Arroyo (Desarrollo Frontend y Experiencia de Usuario)

**Decisiones que tomé y por qué**
- Se diseñó la capa de presentación (`Bellissimo.Web`) con Razor Pages y Bootstrap organizando cada módulo en vistas dedicadas (`Index`, `Nuevo`, `Editar`, `Detalle`, `Eliminar`) con enlace de modelos tipado y validaciones alineadas a `Sql/validaciones_interfaz.md`.

**Dificultades**
- Asegurar que los errores de integridad y reglas de negocio devueltos por MySQL (claves inexistentes en el checador, registros duplicados o semanas cerradas) se mostraran como alertas claras para el usuario sin exponer excepciones técnicas, además de controlar los estados vacíos en reportes y tickets sin incidencias.

**Mi contribución al proyecto**
- Desarrollo de las interfaces web, formularios, navegación, reportes y tickets de pago en `Bellissimo.Web`, así como la ejecución y documentación de las pruebas funcionales de interfaz.

**Lo que aprendí**
- Construir una interfaz web conectada a una arquitectura en capas que respeta la integridad del SGBD y ofrece retroalimentación clara sobre operaciones válidas, inválidas y persistencia de datos.

### Troy Leonardo Moreno Calderón (Arquitectura Backend, Lógica de Negocio y Liderazgo)

**Decisiones que tomé y por qué**
- Se estructuró la solución en cuatro capas (`Web → Negocios → Datos → MySQL`) utilizando ADO.NET con consultas parametrizadas, gestión de credenciales mediante *User Secrets* y una jerarquía de excepciones (`BaseDatosException`, `ErroresBaseDatos`, `ReglaNegocioException`) para traducir códigos `SQLSTATE` hacia la interfaz.

**Dificultades**
- Coordinar la sincronización entre los procedimientos almacenados de MySQL y las llamadas desde `Bellissimo.Datos` para evitar duplicar cálculos o disparos de triggers durante la generación de pagos y el registro de checadas, así como integrar las distintas ramas de trabajo (`backend`, `frontend`, `dba`) en `main` sin conflictos.

**Mi contribución al proyecto**
- Diseño de la arquitectura de software en C#, implementación de las capas `Bellissimo.Entidades`, `Bellissimo.Datos` y `Bellissimo.Negocios`, integración del flujo de nómina semanal, documentación de pruebas de backend (`PRUEBAS_BACKEND.md`) y coordinación general del repositorio.

**Lo que aprendí**
- Desacoplar la lógica de negocio del acceso a datos usando ADO.NET puro, respetar contratos estrictos entre la aplicación y el motor de base de datos, y liderar el flujo de integración continua del equipo con Git y GitHub.