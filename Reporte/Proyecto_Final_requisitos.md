# Proyecto final — Base de datos (UABC, FIAD): requisitos del maestro

Transcripción del PDF `Proyecto_Final.pdf` (4 páginas). Si hay duda, manda el PDF original.

**Propósito.** Integrar lo aprendido mediante análisis, diseño, normalización e implementación de una BD relacional conectada a una aplicación con interfaz gráfica. Justificar decisiones de modelado, demostrar integridad de datos y evidenciar que las operaciones se ejecutan realmente en el SGBD. No se aceptan interfaces simuladas con datos fijos ni archivos locales que sustituyan la BD.

## Etapa 1. Análisis del problema (A1, A2, T1)
1. Describir contexto, problema actual, objetivo, alcance, actores y usuarios.
2. Identificar datos que se registran, decisiones que requieren información y operaciones CRUD previstas.
3. Especificar al menos **ocho reglas de negocio verificables** y **diez requerimientos funcionales** relacionados con la información.
4. Esquema de arquitectura Usuarios → Interfaz/aplicación → SGBD → Base de datos; describir tecnología y mecanismos básicos de acceso, respaldo y seguridad.

## Etapa 2. Diseño conceptual y modelo E–R (A3, A4)
5. Identificar entidades fuertes, débiles y asociativas; clasificar atributos; proponer claves candidatas, primarias y foráneas.
6. Documentar relaciones, cardinalidad, participación, obligatoriedad y reglas de negocio.
7. DER completo en **Crow's Foot** y el mismo modelo en **Classic, Connect to Columns, UML e IDEF1X** (conforme a la Actividad 4).
8. Explicar brevemente qué notación resulta más apropiada y por qué. Diagramas legibles y coherentes entre sí.

## Etapa 3. Diseño relacional, integridad y normalización (A5, A11, A12)
9. Transformar el modelo conceptual en tablas y documentar el **diccionario de datos**: tabla, atributo, significado, tipo, longitud, nulabilidad, PK, FK, valor predeterminado y restricciones.
10. Identificar y justificar integridad de entidad, referencial y de dominio; incluir UNIQUE, NOT NULL, CHECK y claves compuestas cuando proceda.
11. Explicar dependencias funcionales y posibles anomalías de inserción, actualización y eliminación en un esquema inicial o caso representativo.
12. Demostrar la evolución a 1FN, 2FN y 3FN con esquemas, dependencias y explicación de cambios. Si el diseño ya satisface una forma normal, justificarlo en lugar de inventar errores.
13. Analizar aplicabilidad de BCNF, 4FN y 5FN según las reglas de negocio reales; justificar cuando no sean pertinentes y no hacer descomposiciones sin comprobar preservación de información.

## Etapa 4. Álgebra relacional e implementación SQL (A6, A7, A8, A9)
14. Al menos **seis consultas de álgebra relacional** (selección, proyección, JOIN y al menos una operación de conjunto o agrupación); mostrar expresión y resultado esperado.
15. Crear la BD con DDL, tipos adecuados, PK, FK y restricciones; script reproducible de inserción de datos de prueba.
16. Implementar INSERT, SELECT, UPDATE y DELETE con validaciones; demostrar filtrado, ordenamiento, JOIN y consultas con varias tablas.
17. Entregar al menos **diez consultas SQL documentadas**: tres con JOIN, dos subconsultas (incluyendo EXISTS o NOT EXISTS cuando aplique), tres con funciones agregadas y GROUP BY/HAVING, y dos de libre elección de complejidad equivalente.
18. Crear al menos **dos vistas** con propósito funcional y demostrar su consulta. Realizar una **transacción con COMMIT** y un **caso controlado con ROLLBACK**.

## Etapa 5. Programación en la base de datos (A10)
19. Al menos **dos triggers** que hagan cumplir reglas de negocio o registren auditoría. Documentar evento, condición y efecto.
20. Al menos **una función almacenada** y **dos procedimientos almacenados** con parámetros y manejo razonable de errores.
21. Mostrar casos válidos y no válidos que activen estos mecanismos; evitar que la aplicación y los triggers ejecuten dos veces la misma operación.

## Etapa 6. Interfaz y sistema funcional
22. Interfaz navegable con acceso efectivo al SGBD mediante una capa de aplicación o servicio; no exponer credenciales de la BD en el cliente.
23. CRUD completo para al menos **tres entidades relacionadas** y una operación de negocio que afecte dos o más tablas preservando la integridad.
24. Módulo de consulta o reporte con búsqueda/filtros y al menos una consulta multitabla o vista; mensajes de confirmación, error y estados vacíos.
25. Validar campos obligatorios, formatos y reglas de negocio; controlar operaciones inválidas, errores de conexión y conflictos de integridad.
26. Acreditar persistencia: cerrar y reabrir la aplicación, comprobar en el SGBD que los datos se guardaron y que los cambios se reflejan en la interfaz.

## Etapa 7. Pruebas, evidencias y cierre
27. Diseñar y ejecutar al menos **diez casos de prueba**: altas, consultas, modificaciones, eliminación, relaciones, validaciones negativas, triggers, procedimientos, reportes y persistencia.
28. Para cada prueba: identificador, requisito/regla, precondición, datos de entrada, pasos, resultado esperado, **resultado observado** y estado.
29. Documentar al menos **tres incidencias** detectadas y su corrección o justificación si no se presentaron incidencias reales (**sin fabricar resultados**).
30. **Conclusión técnica individual** por integrante: principales decisiones, dificultades, contribución y aprendizaje.

## Requisitos mínimos de implementación
| Elemento | Criterio mínimo |
| --- | --- |
| Base de datos | SGBD relacional (preferentemente MySQL 8); al menos 5 tablas relacionadas y datos de prueba suficientes |
| Diseño | Modelo conceptual y relacional, 5 notaciones de A4, diccionario y análisis de normalización hasta 3FN; evaluación justificada de BCNF/4FN/5FN |
| SQL | DDL/DML, 10 consultas variadas, 2 vistas, operaciones transaccionales, uso correcto de PK/FK |
| Automatización | 2 triggers, 1 función y 2 procedimientos almacenados, demostrados |
| Aplicación | Interfaz funcional, CRUD de 3 entidades, 1 flujo transaccional multitabla, búsqueda y reporte |
| Calidad | 10 pruebas documentadas; validación de errores y evidencia de persistencia |

## Entregables
**A. Reporte técnico en PDF**
31. Portada, índice, introducción y descripción del problema.
32. Objetivos, alcance, usuarios, reglas de negocio y requerimientos.
33. Arquitectura del sistema, descripción del SGBD y tecnologías empleadas.
34. Identificación de entidades y atributos; cinco diagramas de modelado; modelo relacional y diccionario de datos.
35. Análisis de anomalías, dependencias funcionales y normalización (1FN a 3FN; examen de BCNF, 4FN y 5FN).
36. Álgebra relacional: expresiones y resultados; explicación de consultas SQL complejas.
37. Implementación: tablas, restricciones, vistas, triggers, funciones, procedimientos y transacciones.
38. Descripción y capturas de los módulos de interfaz, pruebas, errores, correcciones y resultados.
39. Conclusiones individuales y referencias de consulta cuando corresponda.

**B. Archivos digitales**
- Código fuente completo de la aplicación y README con requisitos, instalación, configuración y ejecución.
- Scripts .sql separados u organizados: 01_esquema, 02_datos, 03_consultas_vistas y 04_rutinas_pruebas (o equivalentes).
- Diagramas originales editables y exportaciones legibles (PDF/PNG).
- Matriz de casos de prueba y resultados; datos de prueba anonimizados o ficticios.
- Video demostrativo de 5 a 8 minutos (interfaz, consultas, persistencia y automatización); no sustituye la defensa presencial.

**C. Presentación y demostración.** Contexto, diseño, normalización, implementación y recorrido en vivo por los módulos principales. Explicar relaciones, ejecutar consultas, mostrar al menos una operación válida y una inválida, comprobar un trigger/procedimiento y verificar los registros persistidos directamente en la BD. Todos los integrantes participan y responden preguntas técnicas.

## Estructura sugerida
`Equipo_XX_ProyectoFinal_BD/` con: Reporte.pdf, Presentacion.pdf, README.md, Aplicacion, Sql, Diagramas, Pruebas, Video.

## Consideraciones de evaluación
- Solución original del equipo y congruente con la documentación.
- No se aceptan diagramas contradictorios, relaciones sin justificación ni normalizaciones aplicadas mecánicamente.
- Los scripts deben ejecutarse sin modificaciones manuales no documentadas.
- Las restricciones críticas se hacen cumplir en la BD y además se validan desde la interfaz cuando proceda.
- La documentación incluye resultados verificables y capturas legibles; se evalúa la capacidad de explicar las decisiones del diseño.
- Fecha, plataforma de entrega, duración de la defensa y equipos: según la indicación publicada por el docente.
