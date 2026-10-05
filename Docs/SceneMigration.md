# Migración de la escena de Última Llamada

## Auditoría del código anterior

Se revisó `DispatchBootstrap.cs` completo antes de sustituirlo. Su `OnEnable` y su `Start` llamaban a `Build`. La escena guardada solo contenía la raíz con ese componente; todos los objetos siguientes se construían después de entrar en Play.

| Responsabilidad anterior | Objetos y componentes creados | Destino |
|---|---|---|
| Puesto | `Central · Puesto 07`, mesa y fondo; MeshFilter, MeshRenderer y primitivas cuyos colliders se eliminaban | Environment: objetos permanentes, materiales guardados |
| Cámara | Main Camera, Camera, AudioListener, DeskCameraController | Operator/Main Camera y cuatro Viewpoints editables |
| Iluminación | Luz direccional de ventana, Light; ambiente plano y sin niebla | Environment/Lighting y RenderSettings de la escena |
| Monitores | Tres grupos: carcasa, soporte, base y LED por monitor | Instancias de Equipment/Monitor.prefab |
| Pantallas | Tres RectTransform, Canvas WorldSpace, GraphicRaycaster, CanvasScaler | Hijos permanentes de los monitores; cada Canvas conserva su cámara |
| Teclado | Cuerpo, 90 teclas, barra de espacio | Equipment/Keyboard.prefab y geometría de escritorio |
| Periféricos | Mousepad, mouse, teléfono con auricular, display y 12 botones; 20 segmentos de banda y dos almohadillas de auriculares | Objetos y prefabs de Environment |
| Micrófono | Instancia del OBJ; ajuste automático de escala/posición/material. Alternativa de tres primitivas si faltaba el modelo. LED aparte | Equipment/Microphone.prefab con transformación, renderer y collider editables; MicrophoneIndicator para el LED |
| Sistemas | GameManager, UnitFleetManager, GeminiEmergencyService, VoiceManager, EmergencyAudioManager en un único objeto | Componentes distribuidos por función: Systems, Units, Operator y Audio |
| Eventos UI | EventSystem e InputSystemUIInputModule si no había uno | UI/EventSystem guardado |
| Vista | AddComponent DispatchView; `Build` construía mapa, llamada, flota, HUD y menú | UI con referencias serializadas a paneles permanentes |

El único modelo que instanciaba el bootstrap era el micrófono OBJ. `monitorModel` y `monitorMaterial` estaban expuestos pero **no se utilizaban**: la geometría de los monitores se hacía con primitivas. Ambulancia, bomberos y policía eran sprites; no existían vehículos 3D ni NPC. No se agregaron simulaciones nuevas durante la migración.

Materiales anteriores: grafito del equipo, aluminio, escritorio, fondo URP Unlit con CentralBackdrop, LED azul, LED rojo del micrófono con emisión y LCD del teléfono; el OBJ usaba Microphone_URP. Se creaban en memoria y se asignaban desde el bootstrap. Ahora los renderers referencian assets `.mat`. Solo el color y emisión del indicador cambian durante la partida, mediante MaterialPropertyBlock sin alterar el asset compartido.

El bootstrap no conectaba eventos de gameplay directamente: delegaba a `GameManager.Initialize`. Este conectaba `relay.Message`, `Voice.CanTalk`, `Voice.BeginTurn`, `Voice.TurnEnded` y `Voice.Problem`. DispatchView registraba acciones de botones, envío de texto, edición de nombre y volumen con lambdas no serializables. Ahora los botones estáticos exponen receptor, acción y argumento en DispatchButton; las entradas de texto y volumen se conectan una vez al inicializar la vista. Los botones del diálogo reutilizado reciben la acción de la confirmación actual.

La evaluación sigue en CallSession/DispatchAudit; UnitFleetManager conserva el estado de los recursos; IncidentMapManager presenta ubicación y zoom; VoiceManager procesa micrófono y reproducción; GeminiEmergencyService transporta los mensajes. GameManager coordina el turno y los conecta, sin construir la escena.

## Escena editable

Abrir `Assets/Scenes/UltimaLlamada.unity`. Environment, Operator, EmergencySystem, Units, UI, Audio y Systems existen antes de Play. El documento `SceneHierarchy.md` enumera **cada GameObject**, sus componentes, scripts, referencias y origen de prefab, exportados de la escena guardada.

Los Canvas WorldSpace están bajo sus monitores para que acompañen sus transformaciones. El avatar es una imagen dentro de CallPanel, porque no hay un personaje 3D del operador en esta versión. Los cinco recursos bajo Units son instancias de prefabs con UnitDefinition; identidad, tipo, base y ETA se editan allí. Su estado de ejecución se clona al iniciar cada guardia para no modificar las definiciones del Inspector.

El menú, sus tres páginas, los cuatro protocolos, el diálogo, el puntaje y el informe son paneles guardados que se activan según el estado. Las filas de recursos también existen desde el Editor. La cantidad de registros de historial depende de las llamadas completadas: se instancia un prefab específico CallHistoryRow por registro. Pin y radar son instancias permanentes de prefabs, ocultas hasta confirmar una ubicación; no necesitan recrearse en cada llamada.

`DispatchBootstrap` se eliminó después de la migración. También se retiraron los dos inicializadores antiguos de audio que creaban objetos y ya no estaban referenciados. Los scripts de construcción usados una sola vez se archivaron fuera de Assets, en `Artifacts/SceneMigrationTools`; no se compilan ni se incluyen en el ejecutable. La acción del menú del Editor únicamente abre la escena guardada: nunca la genera ni sobrescribe los cambios visuales del diseñador. No hay constructores de mundo en Awake o Start.

## Edición habitual

- Mover, rotar o escalar Environment/MonitorMap, MonitorCall, MonitorResources o Microphone desde el Inspector. Sus modelos, renderers y colliders están guardados.
- Editar el material en el renderer o en Assets/Materials/Dispatch; el micrófono conserva su material URP importado.
- Editar encuadres en Operator/Viewpoints. Las posiciones no se reescriben mediante coordenadas rígidas al cargar la escena.
- Editar textos, colores y tamaños en los RectTransform de cada Canvas. Los textos de contenido variable se actualizan durante la partida; la decoración y la composición permanecen como se guardaron.
- Activar temporalmente Results o una página inactiva para editarla y restaurar su estado al guardar. GameManager inicializa los estados de visibilidad al entrar en Play.
- El Inspector de UI/DispatchView incluye botones Escritorio, Menú y Resultados para alternar las vistas de edición sin Play.
- Usar Tools → Última Llamada → Exportar jerarquía y referencias para actualizar el inventario después de editar.

La copia anterior de scripts y escena se conserva en `Artifacts/SceneMigrationBackup` del proyecto principal.

## Verificación

SceneArchitectureValidation abre la escena guardada, comprueba scripts y assets persistentes, prefabs, cámaras, fuentes y filtros de audio. Modifica posición del monitor, escala del micrófono y ETA de una unidad **sin guardar** y verifica que Play respete esos cambios. También verifica que los paneles, las filas de recursos y los protocolos conserven sus objetos al navegar, y que los registros se creen desde el prefab de historial.

LobbyValidation y HudAuditValidation verifican las acciones del menú y el recorrido completo de llamadas, despacho, evidencia de evaluación, informes y reintento. Estas pruebas se ejecutan en la copia aislada de Artifacts/ValidationProject; no usan el micrófono físico ni el servicio real de voz, y no escriben preferencias ni partidas del jugador.

Validación final (Unity 6000.6.0f1): 1089 comprobaciones de arquitectura y referencias; 40 del menú; 17 del flujo de llamadas, puntaje e informes. Compilación de Windows finalizada correctamente con 0 errores. Ejecutable de validación: Artifacts/SceneValidationBuild/UltimaLlamada.exe. Las pruebas de conversación fueron locales.
