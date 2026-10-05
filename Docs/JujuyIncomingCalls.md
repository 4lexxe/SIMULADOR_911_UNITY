# Jujuy: tablero de llamadas

## Uso

Iniciar turno abre el mapa. Seleccionar un punto o una fila muestra su ficha; **Atender llamada** inicia la conversación. M abre o cierra el tablero. Durante una llamada se puede consultar el mapa, pero no sustituirla por otra. Al cerrar la auditoría se vuelve a elegir. Un caso finalizado sigue visible en verde y no vuelve a entrar en espera; Reintentar es la acción explícita para repetirlo.

Tres señales iniciales: San Salvador de Jujuy, Tilcara y La Quiaca. Llegadas posteriores: San Pedro (22 s), Abra Pampa (40 s), Humahuaca (58 s), Purmamarca (76 s) y Libertador General San Martín (94 s). Las llegadas usan tiempo de juego y se pausan en menú/resultados. No se aplica una penalización de puntaje por dejar una señal esperando.

## Geografía y autoría

El mapa usa el polígono de Jujuy y centroides de localidades de **GeoRef Argentina**, descargados el 1 de octubre de 2026. Fuente oficial y formatos: https://www.argentina.gob.ar/georef/descarga-de-la-base-completa . Datos: https://apis.datos.gob.ar/georef/api/v2.0/provincias.geojson y https://apis.datos.gob.ar/georef/api/v2.0/localidades?provincia=jujuy&max=1000 . GeoRef utiliza información geográfica del IGN; la atribución aparece al pie del tablero.

El gráfico es una representación provincial simplificada, con norte arriba, grilla y escala aproximada a 23° S. No usa teselas ni requiere Internet durante el juego. Extensión: oeste −68.05°, este −63.15°, sur −24.7°, norte −21.35°. La fórmula de cada pin es `x=(longitud-oeste)/(este-oeste)` y `y=(latitud-sur)/(norte-sur)`. La imagen y las coordenadas comparten esa misma extensión.

La ubicación indica la **localidad aproximada**, no la posición exacta de una persona. Todos los casos, negocios, direcciones y referencias narrativas son ficticios. Seleccionar una señal no desbloquea el dato `location` ni otorga puntos: debe obtenerse en la conversación. El prompt Live y el ensayo local explican la referencia completa al preguntarla; el mapa no exige adivinar un número de calle.

Fuentes locales: `Artifacts/GeoData/jujuy-boundary.geojson`, `Artifacts/GeoData/localidades.json` y `Artifacts/GeoData/map-extent.json`. La textura incluida es `Assets/UI/Sprites/JujuyMap.png`. `Artifacts/prepare_jujuy.py` registra cómo se prepararon los datos y el gráfico; es una herramienta de autoría, no se ejecuta con el juego. No volver a ejecutarla sobre un catálogo editado sin revisar sus cambios.

## Escena y responsabilidades

- `UI/OperatorCanvas/Mapa provincial de Jujuy`: tablero, mapa recortado, lista de ocho llamadas, ficha, leyenda, atribución y navegación. `IncomingCallBoard` enlaza controles guardados; no construye objetos en Play.
- `IncomingCallMarker.prefab`: punto, halo, número, localidad y botón. Ocho instancias en el tablero y ocho en el monitor. `caseId` conserva la identidad de cada señal.
- `IncomingCallRow.prefab`: nombre, espera/estado y botón. Ocho instancias editables en el tablero.
- `IncomingCallQueue`: reloj y programación de llegadas, independiente del dibujo.
- `GameIncomingCalls`: selección, llamadas pendientes y captura del guardado. `GameManager` conserva conversación, hechos y evaluación.
- `IncidentMapManager`: monitor provincial y pin separado de dirección confirmada. La señal no se convierte en dirección verificada por seleccionarla.
- `JujuyMapNavigation`: zoom, arrastre y límites del mapa. Todas las señales se mueven con el contenido.
- `HudButtonFeedback`: enfoque y presión suaves sobre botones guardados; respeta su escala inicial.
- `EmergencyAudioManager`: tonos de llegada, selección, foco y evaluación. Usa la fuente de efectos existente, separada de voz. Respeta el control de volumen.

No se reconstruyen entorno, equipos, Canvas ni prefabs al iniciar. Se conserva el historial como única lista de objetos instanciada según su cantidad de registros. El editor de `DispatchView` incorpora una vista previa de mapa. El comando `JujuySceneAuthoring.Run` es una herramienta explícita de migración/autoría; no se ejecuta automáticamente y reemplaza su tablero y las tarjetas de casos, por lo que no debe ejecutarse para abrir la escena ni sobre ediciones manuales sin revisar.

## Guardado y evaluación

`GuardSave` versión 3 guarda `queueElapsed`, `priorityCaseId` (caso elegido desde el archivo de prácticas), `selectedCaseId`, resultados en el orden real y estado de flota. Acepta órdenes arbitrarios sin admitir casos duplicados, desconocidos o puntajes fuera de rango. Migra informes v1/v2. Una conversación sin finalizar se reinicia al abrir de nuevo la partida; los resultados completos se conservan.

La puntuación sigue en 40/25/20/10/5 y depende del contenido, no de su longitud. El HUD anima conteo, anillo, barras escalonadas y un pulso final; volver a una pestaña no reproduce la recompensa de un resultado sin cambios. Las llegadas nuevas no alteran una conversación, sus hechos ni su evaluación. Los tiempos de traslado se comprimen 6× por defecto para mantener recursos disponibles durante la guardia; `travelTimeScale` se edita en `Units` y no modifica el reloj de llamada ni la rúbrica.

## Verificación

`JujuyGameplayValidation.Run` carga la escena guardada, pulsa sus botones, comprueba llegada/selección, aislamiento de la llamada activa, conversación local de un nuevo caso, puntaje exacto al terminar la animación, reintento, guardado en orden distinto, migración y continuación. Completa las ocho llamadas y genera capturas a 1080p, 720p y 4:3. Las pruebas Node comprueban el catálogo geográfico y que un nombre de localidad o fragmento de calle no confirme por sí solo una dirección.

Las pruebas automáticas no usan micrófono ni un modelo remoto, y no modifican preferencias ni partidas del jugador.
