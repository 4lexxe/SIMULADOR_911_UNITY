from pathlib import Path
root=Path(__file__).resolve().parents[1]
source=root/'Docs/UltimaLlamada_Examen_Practico.md'
pages=source.read_text(encoding='utf-8').split('<!-- page -->')
pages[0]=pages[0].replace('../Artifacts/lobby-main.png','Figuras/portada_ultima_llamada.png').replace('04.10.2026 · Versión documental 1.2','05.10.2026 · Versión documental 1.3')
pages[1]=pages[1].replace('# El juego y su propósito','# 1. Presentación y GDD').replace('## Intención de triple impacto','## 1.1 Concepto, experiencia e impacto').replace('## Guía de lectura y requisitos del examen','## 1.2 Índice y correspondencia con la consigna').replace('[[10|10 · Implementación]]','[[10|10 · Plan de desarrollo]]')
pages[1]=pages[1].replace('**Público previsto:**','La organización en GDD y LDD adapta el ejemplo académico suministrado [7]. La experiencia buscada combina atención, incertidumbre y satisfacción al decidir con datos.\n\n**Público previsto:**')
titles={2:('# Nivel de examen','# 2. LDD: nivel y personajes'),3:('# Controles y voz','# 3. Controles y tecnología'),4:('# Decidir y actuar','# 4. Reglas y mecánicas'),5:('# Diseño del puesto','# 5. Boceto del nivel'),6:('# Jujuy: ocho llamadas','# 6. Escenarios de Jujuy'),7:('# Una nota que se explica','# 7. Evaluación y progresión'),8:('# Interfaz, arte y sonido','# 8. Arte, interfaz y sonido'),10:('# Pruebas y estado del prototipo','# 10. Pruebas del prototipo'),11:('# Entrega, créditos y fuentes','# 11. Entrega y referencias')}
for idx,(old,new) in titles.items():pages[idx]=pages[idx].replace(old,new)
pages[2]=pages[2].replace('## Una caída en el club · CASE_001','## 2.1 Descripción rápida · Una caída en el club').replace('## Personajes principales','## 2.2 Personajes principales').replace('## Información que debe verificarse','## 2.3 Información del caso').replace('## Límite, cierre y reintento','## 2.4 Duración y progresión')
pages[4]=pages[4].replace('**Objetivo:**','**Oposición:** no hay enemigos ni combate. La dificultad surge de la información incompleta, el tiempo y la disponibilidad de recursos.\n\n**Objetivo:**')
pages[5]='''
# 5. Boceto del nivel
## Cámara, distribución y objetos
El jugador ocupa un puesto fijo con vista en primera persona: mapa a la izquierda, llamada en el centro y unidades a la derecha. La cámara enfoca cada monitor con transiciones; no hay locomoción. Teléfono, teclado, micrófono y auriculares sitúan la acción en una central de emergencias.

![Boceto conceptual generado con IA para Última Llamada: distribución, objetos, luz y flujo del nivel. La ilustración orienta el arte; sus textos de pantalla y cantidades de unidades no definen las reglas implementadas.](Figuras/boceto_nivel_ilustrado.png)

![Implementación real en Unity: monitor de llamada y cinco datos confirmados. Captura de prueba local controlada; los relojes no representan una partida humana cronometrada.](../Artifacts/exam-call.png)

**Del boceto al prototipo:** se conserva la organización mapa → llamada → unidades. La interfaz implementada y el catálogo del juego determinan los datos válidos, los cinco recursos disponibles y la rúbrica descrita en este documento.
'''
pages[9]='''
# 9. Plan de desarrollo
La red PERT y el Gantt modelan un ciclo de construcción del prototipo. Son **estimaciones didácticas en horas**, no un registro histórico ni fechas de trabajo realizadas. Las dependencias representan qué resultado necesita cada actividad para comenzar.

| ID / actividad | Predecesoras | o / m / p → te (h) |
|---|---|---|
| A · Requisitos y alcance | Ninguna | 1 / 2 / 3 → 2 |
| B · Casos y rúbrica | A | 2 / 3 / 4 → 3 |
| C · Escena y recursos | A | 3 / 4 / 5 → 4 |
| D · Diálogo y voz | B | 3 / 5 / 7 → 5 |
| E · Interfaz y mapa | C | 2 / 3 / 4 → 3 |
| F · Integración | D, E | 1 / 2 / 3 → 2 |
| G · Pruebas y correcciones | F | 2 / 3 / 4 → 3 |
| H · Documentación y entrega | G | 1 / 2 / 3 → 2 |

**Cálculo PERT:** te = (o + 4m + p) / 6. o: optimista; m: más probable; p: pesimista. La holgura es inicio tardío menos inicio temprano. Las actividades críticas tienen holgura cero.

![Red PERT calculada. Cada nodo muestra duración esperada y ventana de inicio-fin tempranos. Cian: ruta crítica. Gris: rama con 1 h de holgura.](Figuras/pert_desarrollo.svg)

![Gantt de inicios tempranos derivado de la misma red. El eje muestra horas acumuladas desde el inicio, no fechas reales.](Figuras/gantt_desarrollo.svg)

**Ruta crítica:** A → B → D → F → G → H = **17 h**. C y E tienen 1 h de holgura. El esfuerzo total es **24 horas-persona**: las 17 h suponen capacidad para trabajar en ambas ramas a la vez. Con un único ejecutor y sin solapar trabajo, el mínimo por suma de esfuerzos es 24 h; el calendario se debe ajustar a su disponibilidad.
'''
pages[11]=pages[11].replace('**Alcance actual:** juego individual, ocho situaciones, un nivel de examen y modos local/Live. Quedan fuera rutas geográficas reales, multijugador, procedimientos clínicos, reconocimiento de voz offline, hand tracking y color tracking. La consigna permite elegir una tecnología; se implementó Voice Command. Comisión y docente quedan para completar en portada.','**Arquitectura:** Unity 6000.6.0f1 y C#. La escena guardada separa Environment, Operator, EmergencySystem, Units, UI, Audio y Systems. El modo local funciona con texto; Live utiliza Node.js y Gemini, con credenciales privadas fuera del paquete. Abrir la escena y Play, o la build completa; para Live, configurar y arrancar el backend según README.md.\n\n**Alcance:** juego individual sin rutas reales ni reconocimiento de voz offline. El examen no guarda una partida independiente; la guardia conserva cola, unidades y transcripción, pero no audio. Live transmite audio y texto al servicio de Google. Comisión y docente: por completar.')
pages[11]=pages[11].replace('**[5]** GeoRef','**[7]** *Vibe Coding - Examen Práctico DVJ II*, ejemplo suministrado, 8 páginas. Referencia de estructura GDD/LDD y cronograma; no se reutilizan sus personajes, integrantes ni resultados.\n\n**[5]** GeoRef')
pages[11]=pages[11].replace('Los recursos importados','Los recursos importados').replace('Las capturas de este documento proceden del prototipo; los esquemas explican su diseño.','La portada y el boceto son ilustraciones generadas con IA; las capturas restantes proceden del prototipo.')
gantt='![Gantt de inicios tempranos derivado de la misma red. El eje muestra horas acumuladas desde el inicio, no fechas reales.](Figuras/gantt_desarrollo.svg)'
pages[9]=pages[9].replace(gantt+'\n\n','')
pages[10]='''
# 10. Cronograma y pruebas
El Gantt deriva de la red PERT de la página 10. Las barras cian son críticas y las grises admiten 1 h de holgura. El solapamiento muestra dependencias lógicas; con un solo ejecutor se necesita nivelar el trabajo.

'''+gantt+'''

## Evidencia del prototipo · 04.10.2026
| Verificación | Resultado registrado |
|---|---|
| Examen y comandos / mapa de Jujuy | 30 / 42 comprobaciones aprobadas |
| HUD y evidencias / menú y continuidad | 17 / 40 comprobaciones aprobadas |
| Escena y referencias | 1263 comprobaciones estructurales aprobadas |
| Backend con datos sintéticos | 14 pruebas aprobadas |
| Build Windows / compilación C# | Build correcta; 0 errores y 0 advertencias C# finales |

Se aplicó planificar → preparar → ejecutar → informar → corregir → volver a probar. [3] Las pruebas utilizaron entradas sintéticas y reloj controlado; no hubo validación humana ni micrófono físico. Algunos inicios del Editor emitieron diagnósticos de SearchDatabase sin impedir la build. Reportes: Artifacts/exam-delivery-validation.txt y archivos asociados.

## Defecto corregido · EX-BUG-01
**Categoría:** jugabilidad. **Severidad:** alta. **Versión:** guardia anterior al examen. **Pasos:** iniciar y mantener una llamada abierta. **Observado:** ausencia determinista de cierre a 180 s. **Esperado:** nivel de hasta tres minutos. **Corrección:** Examen / 180 s, un caso y cierre de estados pendientes. **Responsable:** CHANGO DEV. **Estado:** corregido y comprobado automáticamente. [3, p. 14]

## Aceptación todavía pendiente
- Verificar micrófono, cuatro comandos hablados, transcripción y audio en Gemini Live; captura detenida al soltar Espacio o perder foco.
- Medir el cierre con un reloj externo, incluso con menú o grabación activa, y probar la build en la PC de presentación.
- Ensayar con 3 a 5 participantes, dos intentos por persona: registrar decisiones, datos, tiempo, nota y dificultades. No se atribuye eficacia educativa sin ejecutar este estudio.
'''
source.write_text('\n<!-- page -->\n'.join(s.strip() for s in pages)+'\n',encoding='utf-8')

builder=root/'Artifacts/build_exam_document.py'
s=builder.read_text(encoding='utf-8')
s=s.replace("QA=ROOT/'tmp/pdfs/examen12'","QA=ROOT/'tmp/pdfs/examen13'")
s=s.replace("'ARQUITECTURA Y PUESTA EN MARCHA'","'PLANIFICACIÓN · PERT Y GANTT'")
s=s.replace("cap={6:280,7:254,8:240,9:285}","cap={6:247,7:254,8:240,9:285}")
start=s.index('def cover(canvas):')
end=s.index('\ndef page_frame',start)
s=s[:start]+'''def cover(canvas):
    # Preserve the full generated cover, without stretching or cropping its text.
    canvas.setFillColor(NAVY);canvas.rect(0,0,W,H,fill=1,stroke=0)
    path=(SOURCE.parent/cover_image).resolve()
    with PILImage.open(path) as im:iw,ih=im.size
    scale=min(W/iw,H/ih)
    width,height=iw*scale,ih*scale
    canvas.drawImage(str(path),(W-width)/2,(H-height)/2,width=width,height=height,mask='auto')
''' +s[end:]
# Imported diagrams are generated deterministically from their task data.
s=s.replace('DRAWINGS=sketches()',"from planning_diagrams import planning_drawings\nDRAWINGS=sketches()\nDRAWINGS.update(planning_drawings(WIDTH))")
builder.write_text(s,encoding='utf-8')
