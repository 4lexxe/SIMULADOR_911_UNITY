"""Generate an exact high-resolution Gantt image for a single contributor."""
from pathlib import Path
import json
from PIL import Image, ImageDraw, ImageFont

ROOT=Path(__file__).resolve().parents[1]
TASKS=[('A','Requisitos y alcance',2),('B','Casos y rúbrica',3),('C','Escena y recursos',4),('D','Diálogo y voz',5),('E','Interfaz y mapa',3),('F','Integración',2),('G','Pruebas y correcciones',3),('H','Documento y entrega',2)]

def generate():
    factor=3
    im=Image.new('RGB',(1500*factor,790*factor),'white')
    d=ImageDraw.Draw(im)
    fonts={}
    def font(size,bold=False):
        key=(size,bold)
        if key not in fonts:fonts[key]=ImageFont.truetype('C:/Windows/Fonts/'+('trebucbd.ttf' if bold else 'cambria.ttc'),size*factor)
        return fonts[key]
    def text(x,y,value,size=22,bold=False,anchor=None):
        d.text((x*factor,y*factor),value,font=font(size,bold),fill='#000000',anchor=anchor)
    def rect(x,y,w,h,fill,outline=None,width=1):
        d.rectangle((x*factor,y*factor,(x+w)*factor,(y+h)*factor),fill=fill,outline=outline,width=width*factor)
    def line(x1,y1,x2,y2,fill='#d7e2e8',width=1):
        d.line((x1*factor,y1*factor,x2*factor,y2*factor),fill=fill,width=width*factor)
    text(30,18,'ÚLTIMA LLAMADA',35,True)
    text(30,68,'CRONOGRAMA DE GANTT · CHANGO DEV',24,True)
    text(1470,39,'PLAN ESTIMADO',21,True,'ra')
    text(1470,72,'1 persona · 24 horas de trabajo',21,False,'ra')
    left,right=420,1380
    scale=(right-left)/24
    top,rowh=226,56
    text(30,173,'ACTIVIDAD',21,True)
    text(1465,173,'HORAS',21,True,'ra')
    for block in range(6):
        x=left+block*4*scale
        rect(x,132,4*scale,42,'#d7eef7' if block%2==0 else '#edf6fa')
        text(x+2*scale,141,f'BLOQUE {block+1}',18,True,'ma')
    for hour in range(0,25,2):
        x=left+hour*scale
        text(x,185,str(hour),20,False,'ma')
        line(x,218,x,top+len(TASKS)*rowh,'#c3d5de')
    schedule=[];start=0
    for i,(task,name,duration) in enumerate(TASKS):
        y=top+i*rowh
        if i%2==1:rect(20,y,1460,rowh,'#f3f7f9')
        for hour in range(0,25,2):
            x=left+hour*scale;line(x,y,x,y+rowh,'#c3d5de')
        text(32,y+12,f'{task}  {name}',23)
        x=left+start*scale
        rect(x,y+9,duration*scale,36,'#84d5ee',outline='#000000',width=1)
        text(x+duration*scale/2,y+15,f'{duration} h',20,True,'ma')
        text(1450,y+12,str(duration),23,False,'ra')
        schedule.append({'id':task,'task':name,'start':start,'end':start+duration,'hours':duration})
        start+=duration
    assert start==24
    assert all(schedule[i]['end']==schedule[i+1]['start'] for i in range(len(schedule)-1))
    line(20,684,1480,684,'#000000')
    text(30,705,'Escala: horas acumuladas de trabajo · Cada bloque equivale a 4 horas.',22)
    text(30,744,'Plan de referencia: actividades secuenciales, sin fechas reales ni solapamiento.',20)
    out=ROOT/'Docs/Figuras/gantt_ultima_llamada.png';out.parent.mkdir(exist_ok=True)
    im.save(out,dpi=(300,300))
    (ROOT/'Artifacts/gantt-validation.json').write_text(json.dumps({'type':'estimated_single_contributor_schedule','total_hours':24,'blocks_of_4_hours':6,'tasks':schedule,'image_px':im.size,'text_color':'#000000'},ensure_ascii=False,indent=2),encoding='utf-8')
    return out

if __name__=='__main__':print(generate())
