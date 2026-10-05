"""Build the 12-page academic PDF from the editable Markdown source."""
from pathlib import Path
import re, html, json
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.lib import colors
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.pagesizes import A4
from reportlab.platypus import BaseDocTemplate, PageTemplate, Frame, Paragraph, Spacer, Table, TableStyle, Image, PageBreak, KeepTogether, Flowable
from reportlab.graphics.shapes import Drawing, Rect, Line, String, Polygon, Circle
from reportlab.graphics import renderSVG
from PIL import Image as PILImage
from pypdf import PdfReader

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'Docs/UltimaLlamada_Examen_Practico.md'
OUTPUT=ROOT/'output/pdf/UltimaLlamada_Examen_Practico_CHANGO_DEV.pdf'
QA=ROOT/'tmp/pdfs/examen15'
FIGURES=ROOT/'Docs/Figuras'
for directory in [OUTPUT.parent,QA,FIGURES]: directory.mkdir(parents=True,exist_ok=True)
for name,file in [('Body','cambria.ttc'),('Body-Bold','cambriab.ttf'),('Body-Italic','cambriai.ttf'),('Display','trebucbd.ttf')]:
    pdfmetrics.registerFont(TTFont(name,str(Path('C:/Windows/Fonts')/file)))
pdfmetrics.registerFontFamily('Body',normal='Body',bold='Body-Bold',italic='Body-Italic',boldItalic='Body-Bold')
INK=colors.black
NAVY=colors.HexColor('#061c30')
ACCENT=colors.black
CYAN=colors.HexColor('#35d0ee')
GRAY=colors.HexColor('#d5e1e7')
MUTED=colors.black
PALE=colors.HexColor('#eff6f8')
W,H=A4
MARGIN=44
WIDTH=W-2*MARGIN
styles={
    'body':ParagraphStyle('body',fontName='Body',fontSize=10.5,leading=14,textColor=INK,spaceAfter=8),
    'h1':ParagraphStyle('h1',fontName='Display',fontSize=22,leading=25,textColor=INK,spaceAfter=13),
    'h2':ParagraphStyle('h2',fontName='Display',fontSize=12,leading=16,textColor=INK,spaceBefore=8,spaceAfter=7,keepWithNext=True),
    'kicker':ParagraphStyle('kicker',fontName='Display',fontSize=8.5,leading=12,textColor=ACCENT,spaceAfter=6),
    'caption':ParagraphStyle('caption',fontName='Body',fontSize=8.4,leading=11.2,textColor=MUTED,spaceAfter=11),
    'cell':ParagraphStyle('cell',fontName='Body',fontSize=9.5,leading=12.4,textColor=INK),
    'th':ParagraphStyle('th',fontName='Display',fontSize=9.5,leading=12.4,textColor=INK),
    'bullet':ParagraphStyle('bullet',fontName='Body',fontSize=10.5,leading=14,textColor=INK,leftIndent=13,firstLineIndent=-11,spaceAfter=6),
    'callout':ParagraphStyle('callout',fontName='Body',fontSize=10.2,leading=14.3,textColor=INK),
}
def rich(value):
    value=html.escape(value.replace('–','-').replace('—','-').replace('\u2011','-'))
    value=re.sub(r'\*\*(.*?)\*\*',r'<b>\1</b>',value)
    value=re.sub(r'(?<!\*)\*([^*]+)\*(?!\*)',r'<i>\1</i>',value)
    value=re.sub(r'\[\[(\d+)\|(.*?)\]\]',r'<link href="#section-\1" color="#000000">\2</link>',value)
    value=re.sub(r'\[([^\[\]]+)\]\((https://[^\s]+)\)',r'<link href="\2" color="#000000"><u>\1</u></link>',value)
    return value
def p(value,style='body'): return Paragraph(rich(value),styles[style])
def label(d,x,y,value,size=10,bold=False,color=INK,anchor='start'):
    d.add(String(x,y,value,fontName='Body-Bold' if bold else 'Body',fontSize=size,fillColor=color,textAnchor=anchor))
def box(d,x,y,w,h,title,lines,dark=False):
    d.add(Rect(x,y,w,h,fillColor=NAVY if dark else PALE,strokeColor=GRAY,strokeWidth=.7))
    label(d,x+10,y+h-19,title,10,True,CYAN if dark else ACCENT)
    for i,line in enumerate(lines): label(d,x+10,y+h-40-i*15,line,9,False,colors.white if dark else INK)
def arrow(d,x,y,x2,y2):
    d.add(Line(x,y,x2,y2,strokeColor=ACCENT,strokeWidth=1.4))
    d.add(Polygon([x2,y2,x2-5,y2+3,x2-5,y2-3],fillColor=ACCENT,strokeColor=None))
def sketches():
    d=Drawing(WIDTH,190)
    d.add(Rect(0,166,WIDTH,24,fillColor=NAVY,strokeColor=None))
    label(d,WIDTH/2,174,'E · HUD / ESTADO DE ATENCIÓN / EXAMEN 03:00',10,True,CYAN,'middle')
    size=(WIDTH-22)/3
    for i,(title,lines) in enumerate([
      ('A · MAPA DE JUJUY',['Señales por localidad','Ficha de la llamada','Seleccionar / atender']),
      ('B · LLAMADA',['Transcripción y respuesta','Cinco datos por confirmar','Atender / finalizar']),
      ('C · UNIDADES',['Tipo y disponibilidad','Estado y tiempo estimado','Asignar / confirmar'])]):
        x=i*(size+11);box(d,x,62,size,90,title,lines)
        d.add(Line(x+size/2,62,x+size/2,47,strokeColor=GRAY,strokeWidth=4))
    d.add(Line(0,44,WIDTH,44,strokeColor=INK,strokeWidth=2))
    label(d,WIDTH/2,25,'D · JUGADOR / PUESTO FIJO',10,True,INK,'middle')
    label(d,WIDTH/2,6,'Teléfono · teclado · micrófono · auriculares',9,False,MUTED,'middle')
    a=Drawing(WIDTH,163)
    a.add(Rect(0,0,WIDTH,163,fillColor=PALE,strokeColor=None))
    label(a,15,143,'UltimaLlamada / escena guardada',11,True)
    groups=[('Environment','puesto y tres monitores'),('Operator','cámara, vistas y voz'),('EmergencySystem','mapa de incidentes'),('Units','cinco recursos editables'),('UI','lobby, mapa, HUD y auditoría'),('Audio','efectos sonoros'),('Systems','GameManager y relay')]
    a.add(Line(21,128,21,15,strokeColor=ACCENT))
    for i,(title,desc) in enumerate(groups):
        y=122-i*18;a.add(Line(21,y+3,34,y+3,strokeColor=ACCENT));label(a,42,y,title,9.6,True);label(a,192,y,desc,9.6)
    v=Drawing(WIDTH,100)
    nodes=[('JUGADOR',['Micrófono','Espacio sostenido']),('UNITY',['VoiceManager','Transcripción']),('RELAY LOCAL',['Node / WebSocket','Valida eventos']),('GEMINI LIVE',['Voz del llamante','Texto y propuestas'])]
    bw=(WIDTH-30)/4
    for i,(title,lines) in enumerate(nodes):
        x=i*(bw+10);box(v,x,29,bw,71,title,lines)
        if i<3:arrow(v,x+bw,62,x+bw+9,62)
    label(v,WIDTH/2,10,'Retorno: audio + texto + eventos; Unity valida el estado.',9.5,True,ACCENT,'middle')
    t=Drawing(WIDTH,75)
    cols=[('00-15 s','Elegir y atender'),('15-110 s','Escuchar y verificar'),('110-150 s','Confirmar y enviar'),('150-180 s','Comunicar y cerrar')]
    size=WIDTH/4
    for i,(when,what) in enumerate(cols):
        x=i*size;t.add(Rect(x,20,size-4,55,fillColor=PALE,strokeColor=None))
        t.add(Rect(x,71,size-4,4,fillColor=ACCENT if i<3 else colors.HexColor('#ca8c16'),strokeColor=None))
        label(t,x+9,51,when,11,True)
        label(t,x+9,33,what,8.8)
    label(t,WIDTH/2,3,'180 segundos como máximo · cierre anticipado permitido',9.5,True,ACCENT,'middle')
    return {'boceto_nivel.svg':d,'jerarquia.svg':a,'flujo_voz.svg':v,'ritmo_nivel.svg':t}
from gantt_chart import generate as generate_gantt
generate_gantt()
DRAWINGS=sketches()
for name,drawing in DRAWINGS.items(): renderSVG.drawToFile(drawing,str(FIGURES/name))
text=SOURCE.read_text(encoding='utf-8')
pages=text.split('<!-- page -->')
assert len(pages)==12, 'Source must have exactly 12 sections including cover.'
metadata=dict(re.findall(r'^\*\*(.+?):\*\* (.+)$',pages[0],re.M))
cover_image=re.search(r'!\[.*?\]\((.*?)\)',pages[0]).group(1)
headings=[]
KICKERS=['PORTADA','ÍNDICE Y PRESENTACIÓN','DISEÑO DEL NIVEL','ENTRADAS Y TECNOLOGÍA','OBJETIVOS Y MECÁNICAS','BOCETO E IMPLEMENTACIÓN','TERRITORIO Y CONTENIDO','EVALUACIÓN DEL DESEMPEÑO','EXPERIENCIA DE JUEGO','PLANIFICACIÓN DEL PROYECTO','CONTROL DE CALIDAD','CRÉDITOS Y REFERENCIAS']
class Heading(KeepTogether):
    def __init__(self,number,title):
        marker=p(f'{number:02d}  /  {KICKERS[number-1]}','kicker')
        marker._section=(number,title)
        super().__init__([marker,p(title,'h1')])
class Cover(Flowable):
    def __init__(self): super().__init__();self.width=WIDTH;self.height=1;self._section=(1,'Portada')
    def draw(self): pass
def table(lines):
    rows=[]
    for line in lines:
        line=re.sub(r'\[\[(\d+)\|(.*?)\]\]',lambda m:'[['+m[1]+'§'+m[2]+']]',line)
        parts=[s.strip().replace('§','|') for s in line.strip().strip('|').split('|')]
        if all(re.fullmatch(r'[-: ]+',x or '-') for x in parts): continue
        rows.append([p(s,'th' if not rows else 'cell') for s in parts])
    n=len(rows[0]);head=lines[0]
    if n==2: widths=[WIDTH*.38,WIDTH*.62]
    elif 'Máximo' in head: widths=[WIDTH*.25,WIDTH*.11,WIDTH*.64]
    elif 'Caso' in head: widths=[WIDTH*.34,WIDTH*.47,WIDTH*.19]
    else: widths=[WIDTH*.25,WIDTH*.39,WIDTH*.36]
    if 'Verificación' in head: widths=[WIDTH*.61,WIDTH*.39]
    if 'Consigna' in head: widths=[WIDTH*.51,WIDTH*.49]
    t=Table(rows,colWidths=widths,hAlign='LEFT',repeatRows=1)
    t.setStyle(TableStyle([
       ('VALIGN',(0,0),(-1,-1),'TOP'),('BACKGROUND',(0,0),(-1,0),colors.HexColor('#d7eef7')),
       ('ROWBACKGROUNDS',(0,1),(-1,-1),[colors.white,PALE]),
       ('LINEBELOW',(0,1),(-1,-1),.4,GRAY),
       ('LEFTPADDING',(0,0),(-1,-1),8),('RIGHTPADDING',(0,0),(-1,-1),8),
       ('TOPPADDING',(0,0),(-1,-1),6),('BOTTOMPADDING',(0,0),(-1,-1),6)
    ]))
    return [t,Spacer(1,10)]
def callout(value):
    t=Table([[p(value,'callout')]],colWidths=[WIDTH])
    t.setStyle(TableStyle([('BACKGROUND',(0,0),(-1,-1),PALE),('LINEBEFORE',(0,0),(0,-1),3,ACCENT),('LEFTPADDING',(0,0),(-1,-1),12),('RIGHTPADDING',(0,0),(-1,-1),10),('TOPPADDING',(0,0),(-1,-1),10),('BOTTOMPADDING',(0,0),(-1,-1),10)]))
    return [t,Spacer(1,8)]
class Contents(Flowable):
    def __init__(self):
        super().__init__();self.width=WIDTH
        self.entries=[(2,'1. Presentación y GDD')]
        for number,section in enumerate(pages[2:],3):
            title=re.search(r'^# (.+)$',section,re.M).group(1)
            self.entries.append((number,title))
        self.height=len(self.entries)*18+17
    def draw(self):
        c=self.canv;c.saveState();c.setFillColor(colors.black)
        for i,(number,title) in enumerate(self.entries):
            y=self.height-14-i*18
            c.setFont('Body',11);c.drawString(0,y,title)
            start=pdfmetrics.stringWidth(title,'Body',11)+9
            dots=max(0,int((WIDTH-29-start)/pdfmetrics.stringWidth('.','Body',9)))
            c.setFont('Body',9);c.drawString(start,y,'.'*dots)
            c.setFont('Body-Bold',11);c.drawRightString(WIDTH,y,str(number))
            c.linkRect('',f'section-{number}',(0,y-3,WIDTH,y+12),relative=1,thickness=0)
        c.restoreState()

story=[Cover()]
figure_count=0
for page_i,section in enumerate(pages[1:],2):
    story.append(PageBreak())
    lines=section.strip().splitlines();i=0
    while i<len(lines):
        line=lines[i].strip();i+=1
        if not line: continue
        if line=='{{INDICE}}':story.append(Contents());continue
        if line.startswith('# '):story.append(Heading(page_i,line[2:]));continue
        if line.startswith('## '):story.append(p(line[3:],'h2'));continue
        if line.startswith('|'):
            block=[line]
            while i<len(lines) and lines[i].strip().startswith('|'):block.append(lines[i]);i+=1
            story.extend(table(block));continue
        match=re.fullmatch(r'!\[(.*?)\]\((.*?)\)',line)
        if match:
            caption,path=match.groups();asset=(SOURCE.parent/path).resolve();figure_count+=1
            if asset.suffix=='.svg': fig=DRAWINGS[asset.name]
            else:
                with PILImage.open(asset) as im: iw,ih=im.size
                cap={6:207,7:238,8:224,9:255,10:310}.get(page_i,285)
                width=min(WIDTH,cap*iw/ih)
                fig=Image(str(asset),width=width,height=width*ih/iw)
            story.append(KeepTogether([fig,Spacer(1,6),p(f'**FIG. {figure_count:02d}**  {caption}','caption')]))
            continue
        if line.startswith('> '):story.extend(callout(line[2:]));continue
        if line.startswith('- '):story.append(p('• '+line[2:],'bullet'));continue
        if re.match(r'^\d+\. ',line):story.append(p(line,'bullet'));continue
        paragraph=[line]
        while i<len(lines) and lines[i].strip() and not lines[i].startswith(('#','|','!','- ','> ')) and not re.match(r'^\d+\. ',lines[i]):
            paragraph.append(lines[i].strip());i+=1
        story.append(p(' '.join(paragraph)))

def cover(canvas):
    # Preserve the full generated cover, without stretching or cropping its text.
    canvas.setFillColor(NAVY);canvas.rect(0,0,W,H,fill=1,stroke=0)
    path=(SOURCE.parent/cover_image).resolve()
    with PILImage.open(path) as im:iw,ih=im.size
    scale=min(W/iw,H/ih)
    width,height=iw*scale,ih*scale
    canvas.drawImage(str(path),(W-width)/2,(H-height)/2,width=width,height=height,mask='auto')

def page_frame(canvas,doc):
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

class Document(BaseDocTemplate):
    def afterFlowable(self,flowable):
        if hasattr(flowable,'_section'):
            expected,title=flowable._section
            headings.append({'expected':expected,'actual':self.page,'title':title})
            key=f'section-{expected}';self.canv.bookmarkPage(key);self.canv.addOutlineEntry(title,key,0,False)
doc=Document(str(OUTPUT),pagesize=A4,title='Última Llamada | Documento de diseño y nivel',author='CHANGO DEV · Alex Gabriel Calatayud',subject='Primer Examen Práctico · Diseño de Videojuegos II · UNJu · 2026')
frame=Frame(44,49,WIDTH,H-153,leftPadding=0,rightPadding=0,topPadding=0,bottomPadding=0)
doc.addPageTemplates(PageTemplate(id='document',frames=frame,onPage=page_frame))
doc.build(story)
reader=PdfReader(OUTPUT)
report={'pages':len(reader.pages),'max_pages':12,'sections':headings,'page_mismatches':[h for h in headings if h['expected']!=h['actual']],'raster_images_including_cover':len(re.findall(r'!\[.*?\]\(.*?\.png\)',text)),'figures':figure_count,'body_font_pt':10.5,'table_font_pt':9.5}
(QA/'document-layout.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
(ROOT/'Artifacts/exam-document-layout.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(report,ensure_ascii=False,indent=2))
assert report['pages']==12, 'The exported PDF must contain exactly 12 pages including the cover.'
assert not report['page_mismatches'], 'A section overflowed its assigned page.'
