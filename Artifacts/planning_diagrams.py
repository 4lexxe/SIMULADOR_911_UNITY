"""PERT expectations, forward/backward passes and consistent Gantt drawings."""
from pathlib import Path
import json, math
from reportlab.graphics.shapes import Drawing, Rect, Line, String, Polygon
from reportlab.lib import colors

TASKS=[('A',[],1,2,3),('B',['A'],2,3,4),('C',['A'],3,4,5),('D',['B'],3,5,7),('E',['C'],2,3,4),('F',['D','E'],1,2,3),('G',['F'],2,3,4),('H',['G'],1,2,3)]
def calculate():
    data={}
    for name,preds,o,m,p in TASKS:
        duration=(o+4*m+p)/6
        start=max([data[x]['ef'] for x in preds],default=0)
        data[name]=dict(predecessors=preds,o=o,m=m,p=p,te=duration,es=start,ef=start+duration)
    finish=max(v['ef'] for v in data.values())
    for name in reversed(data):
        successors=[v for v in data.values() if name in v['predecessors']]
        last=min([v['ls'] for v in successors],default=finish)
        data[name].update(lf=last,ls=last-data[name]['te'])
        data[name]['slack']=data[name]['ls']-data[name]['es']
    assert finish==17 and sum(v['te'] for v in data.values())==24
    assert [k for k,v in data.items() if v['slack']==0]==['A','B','D','F','G','H']
    for v in data.values():
        assert v['slack']>=0
        assert all(data[p]['ef']<=v['es'] for p in v['predecessors'])
    return data

def planning_drawings(width):
    data=calculate()
    (Path(__file__).parent/'planning-validation.json').write_text(json.dumps({'status':'estimated_model_not_actual_times','unit':'hours','tasks':data,'critical_path':['A','B','D','F','G','H'],'duration':17,'total_effort_person_hours':24},indent=2),encoding='utf-8')
    blue=colors.HexColor('#007c9a');navy=colors.HexColor('#061c30');gray=colors.HexColor('#647d8d');pale=colors.HexColor('#e7eff3')
    def txt(d,x,y,t,size=9,color=navy,bold=False,anchor='start'):
        d.add(String(x,y,t,fontName='Body-Bold' if bold else 'Body',fontSize=size,fillColor=color,textAnchor=anchor))
    d=Drawing(width,148)
    bw=72;gap=(width-6*bw)/5;step=bw+gap
    positions={'A':(0,54),'B':(step,102),'C':(step,6),'D':(step*2,102),'E':(step*2,6),'F':(step*3,54),'G':(step*4,54),'H':(step*5,54)}
    for k,v in data.items():
        x2,y2=positions[k];end=(x2,y2+20)
        for pred in v['predecessors']:
            x,y=positions[pred];start=(x+bw,y+20)
            critical=data[pred]['slack']==0 and v['slack']==0
            color=blue if critical else gray
            d.add(Line(*start,*end,strokeColor=color,strokeWidth=1.8 if critical else 1))
            angle=math.atan2(end[1]-start[1],end[0]-start[0]);ux,uy=math.cos(angle),math.sin(angle)
            ex,ey=end
            d.add(Polygon([ex,ey,ex-6*ux+3*uy,ey-6*uy-3*ux,ex-6*ux-3*uy,ey-6*uy+3*ux],fillColor=color,strokeColor=None))
    for k,v in data.items():
        x,y=positions[k];critical=v['slack']==0
        d.add(Rect(x,y,bw,40,fillColor=blue if critical else pale,strokeColor=blue if critical else gray,strokeWidth=.7))
        color=colors.white if critical else navy
        txt(d,x+bw/2,y+25,f'{k}  /  {v["te"]:g} h',10,color,True,'middle')
        txt(d,x+bw/2,y+10,f'{v["es"]:g} - {v["ef"]:g} h',8.5,color,False,'middle')
    g=Drawing(width,128)
    left=37;scale=(width-left-8)/17
    for hour in range(18):
        x=left+hour*scale;g.add(Line(x,5,x,105,strokeColor=pale,strokeWidth=.5))
        txt(g,x,113,str(hour),8,gray,False,'middle')
    for i,(k,v) in enumerate(data.items()):
        y=94-i*12.5;txt(g,8,y+1,k,9,navy,True)
        g.add(Rect(left+v['es']*scale,y,v['te']*scale,9,fillColor=blue if v['slack']==0 else gray,strokeColor=None))
    return {'pert_desarrollo.svg':d,'gantt_desarrollo.svg':g}
