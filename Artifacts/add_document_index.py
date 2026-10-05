from pathlib import Path
root=Path(__file__).resolve().parents[1]
p=root/'Docs/UltimaLlamada_Examen_Practico.md'
sections=p.read_text(encoding='utf-8').split('<!-- page -->')
old=sections[1]
concept=old[old.index('Última Llamada es'):old.index('## 1.2 Índice')]
sections[1]='# Índice\n\n{{INDICE}}\n\n## 1. Presentación y GDD\n'+concept
sections[1]=sections[1].replace('La organización en GDD y LDD adapta el ejemplo académico suministrado [7]. La experiencia buscada combina atención, incertidumbre y satisfacción al decidir con datos.\n\n','')
sections[0]=sections[0].replace('Versión documental 1.4','Versión documental 1.5')
p.write_text('\n<!-- page -->\n'.join(s.strip() for s in sections)+'\n',encoding='utf-8')
p=root/'Artifacts/build_exam_document.py';s=p.read_text(encoding='utf-8')
s=s.replace("QA=ROOT/'tmp/pdfs/examen14'","QA=ROOT/'tmp/pdfs/examen15'")
s=s.replace("'PORTADA','CONCEPTO Y CONSIGNA'","'PORTADA','ÍNDICE Y PRESENTACIÓN'")
insert='''class Contents(Flowable):
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

'''
s=s.replace('story=[Cover()]',insert+'story=[Cover()]')
s=s.replace("if not line: continue\n        if line.startswith('# ')","if not line: continue\n        if line=='{{INDICE}}':story.append(Contents());continue\n        if line.startswith('# ')")
p.write_text(s,encoding='utf-8')
