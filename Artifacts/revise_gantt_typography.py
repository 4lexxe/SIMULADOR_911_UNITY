from pathlib import Path
import re
ROOT=Path(__file__).resolve().parents[1]
source=ROOT/'Docs/UltimaLlamada_Examen_Practico.md'
pages=source.read_text(encoding='utf-8').split('<!-- page -->')
pages[0]=pages[0].replace('Versión documental 1.3','Versión documental 1.4')
pages[9]='''
# 9. Cronograma de desarrollo
El Gantt organiza las actividades de Última Llamada para **Alex Gabriel Calatayud, integrante de CHANGO DEV**. Se plantea una secuencia de trabajo para una persona: una actividad comienza cuando termina la anterior.

![Cronograma de Gantt del proyecto. Imagen generada a partir de la tabla de actividades: ocho tareas, seis bloques de cuatro horas y 24 horas estimadas en total.](Figuras/gantt_ultima_llamada.png)

## Cómo leer el cronograma
Cada fila representa una actividad y cada barra indica su inicio, duración y final. La escala superior expresa horas acumuladas de trabajo. Los seis bloques agrupan cuatro horas cada uno; no equivalen a fechas del calendario ni a jornadas completas.

## Resultados previstos por etapa
| Etapa | Resultado a obtener |
|---|---|
| A-B · Definición | Alcance del nivel, ocho casos ficticios y criterios de evaluación. |
| C-D · Construcción | Puesto de tres monitores, recursos, diálogo y entrada de voz. |
| E-F · Experiencia e integración | Mapa de Jujuy, HUD, menú y conexión entre los sistemas. |
| G-H · Verificación y entrega | Pruebas del prototipo, correcciones, documentación y build. |

**Alcance de la estimación:** 24 horas de trabajo efectivo. Es una planificación orientativa del ciclo de desarrollo, no un registro de horas ya realizadas. Los tiempos de aprendizaje, espera de servicios y disponibilidad personal pueden ampliar el calendario. Las pruebas efectivamente registradas se describen en la página siguiente.
'''
pages[10]=pages[10][pages[10].index('## Evidencia del prototipo'):]
pages[10]='# 10. Pruebas del prototipo\n\n'+pages[10]
pages[10]+='''

## Criterio de aceptación
Antes de presentar, comprobar que se puede iniciar el examen, atender el caso, registrar datos, confirmar una unidad y llegar al informe. La prueba oral debe verificar los cuatro comandos y el corte de captura; la prueba temporal debe confirmar el cierre de 180 segundos. Registrar cada resultado como aprobado, fallido o pendiente, con observaciones.
'''
assert all('PERT' not in section for section in pages)
source.write_text('\n<!-- page -->\n'.join(s.strip() for s in pages)+'\n',encoding='utf-8')

builder=ROOT/'Artifacts/build_exam_document.py'
s=builder.read_text(encoding='utf-8')
s=s.replace("QA=ROOT/'tmp/pdfs/examen13'","QA=ROOT/'tmp/pdfs/examen14'")
s=s.replace("[('Body','arial.ttf'),('Body-Bold','arialbd.ttf'),('Body-Italic','ariali.ttf')]","[('Body','cambria.ttc'),('Body-Bold','cambriab.ttf'),('Body-Italic','cambriai.ttf'),('Display','trebucbd.ttf')]")
s=s.replace("INK=colors.HexColor('#172b3c')","INK=colors.black").replace("ACCENT=colors.HexColor('#007c9a')","ACCENT=colors.black").replace("MUTED=colors.HexColor('#536979')","MUTED=colors.black")
s=s.replace('color="#007c9a"','color="#000000"')
s=s.replace("fontName='Body-Bold',fontSize=24,leading=28,textColor=NAVY,spaceAfter=18","fontName='Display',fontSize=22,leading=25,textColor=INK,spaceAfter=13")
s=s.replace("fontName='Body-Bold',fontSize=12","fontName='Display',fontSize=12")
s=s.replace("fontName='Body-Bold',fontSize=8.5","fontName='Display',fontSize=8.5")
s=s.replace("'th':ParagraphStyle('th',fontName='Body-Bold',fontSize=9.5,leading=12.4,textColor=colors.white)","'th':ParagraphStyle('th',fontName='Display',fontSize=9.5,leading=12.4,textColor=INK)")
s=s.replace("fontSize=10.5,leading=14.5","fontSize=10.5,leading=14")
s=s.replace("('BACKGROUND',(0,0),(-1,0),NAVY)","('BACKGROUND',(0,0),(-1,0),colors.HexColor('#d7eef7'))")
s=s.replace("from planning_diagrams import planning_drawings\nDRAWINGS=sketches()\nDRAWINGS.update(planning_drawings(WIDTH))","from gantt_chart import generate as generate_gantt\ngenerate_gantt()\nDRAWINGS=sketches()")
s=s.replace('PLANIFICACIÓN · PERT Y GANTT','PLANIFICACIÓN DEL PROYECTO')
s=s.replace('cap={6:220,7:254,8:240,9:285}','cap={6:207,7:238,8:224,9:255,10:310}')
start=s.index('def page_frame(canvas,doc):')
end=s.index('\nclass Document',start)
s=s[:start]+'''def page_frame(canvas,doc):
    canvas.saveState()
    if doc.page==1:cover(canvas)
    else:
        # Full academic masthead, separate from the body frame.
        top=H-22;bottom=H-88
        canvas.setStrokeColor(colors.black);canvas.setLineWidth(.7)
        canvas.rect(44,bottom,WIDTH,66,fill=0,stroke=1)
        canvas.line(117,bottom,117,top);canvas.line(W-107,bottom,W-107,top)
        canvas.setFillColor(colors.black)
        canvas.setFont('Display',20);canvas.drawCentredString(80.5,H-47,'UNJu')
        canvas.setFont('Body',8.5);canvas.drawCentredString(80.5,H-62,'Facultad de')
        canvas.drawCentredString(80.5,H-74,'Ingeniería')
        cx=(117+W-107)/2
        canvas.setFont('Display',12);canvas.drawCentredString(cx,H-38,'Diseño de Videojuegos II')
        canvas.setFont('Body',8.4);canvas.drawCentredString(cx,H-52,'Tecnicatura Universitaria en Diseño Integral de Videojuegos')
        canvas.setFont('Body-Bold',9);canvas.drawCentredString(cx,H-67,'Primer Examen Práctico · Última Llamada')
        canvas.setFont('Body',8.8);canvas.drawCentredString(cx,H-80,'CHANGO DEV · Alex Gabriel Calatayud')
        cx=W-75.5
        canvas.setFont('Body',8.5);canvas.drawCentredString(cx,H-39,'Período')
        canvas.drawCentredString(cx,H-51,'lectivo')
        canvas.setFont('Display',15);canvas.drawCentredString(cx,H-74,'2026')
        canvas.setStrokeColor(colors.black);canvas.line(44,39,W-44,39)
        canvas.setFont('Body',8);canvas.drawString(44,25,'ÚLTIMA LLAMADA · CHANGO DEV · DISEÑO DE VIDEOJUEGOS II')
        canvas.setFont('Display',9);canvas.drawRightString(W-44,25,f'{doc.page:02d} / 12')
        canvas.linkRect('', 'section-2', (44,19,385,34),relative=0,thickness=0)
    canvas.restoreState()
''' +s[end:]
s=s.replace('frame=Frame(44,57,WIDTH,H-114','frame=Frame(44,49,WIDTH,H-153')
s=s.replace("'screenshots_including_cover':len(re.findall(r'!\\[.*?\\]\\(.*?\\.png\\)',text))","'raster_images_including_cover':len(re.findall(r'!\\[.*?\\]\\(.*?\\.png\\)',text))")
builder.write_text(s,encoding='utf-8')
