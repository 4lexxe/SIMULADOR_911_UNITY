from pathlib import Path
import re
b=Path(r'H:\UNITY\SERIUS\Artifacts\SceneMigrationBackup\Scripts')
p=Path(r'H:\UNITY\SERIUS\Artifacts\ValidationProject\Assets\Scripts\Authoring\DispatchView.ResultsAuthoring.cs')
s=(b/'DispatchResultView.cs').read_text(encoding='utf-8-sig')
head=s[s.index('            var panel=Rect(overlay'):s.index('            if(resultTab==1)')]
head=head.replace('resultPanel=panel;','resultRoot=panel; resultGroup=panel.gameObject.AddComponent<CanvasGroup>();')
head=head.replace('            Label(header.transform,105','            resultHeader=Label(header.transform,105')
head=head.replace('if(!shift)Label(header.transform,730','resultCase=Label(header.transform,730')
report=s[s.index('                var report=Angular'):s.index('                var copy=new StringBuilder();')]
report=report.replace('                Glyph(report.transform','                resultReport=report.gameObject;\n                Glyph(report.transform')
report+='''            reportCopy=ScrollCopy(report.transform,28,84,1416,742,"Informe de la llamada",21,Color.white,"Informe desplazable");
            var summaryRoot=Rect(panel,0,0,1520,1024,"Puntaje y observaciones"); resultSummary=summaryRoot.gameObject;
'''
summary=s[s.index('            Color gradeColor='):s.index('            ResultActions(panel,shift);\n            var cg=')]
summary=summary.replace('Angular(panel,','Angular(summaryRoot,').replace('BuildNotes(panel,','BuildNotes(summaryRoot,')
summary=summary.replace('var ring=Box(', 'var ring=resultRing=Box(').replace('var score=Label(', 'var score=resultScore=Label(').replace('var grade=Label(', 'var grade=resultGrade=Label(')
summary=summary.replace('            Label(summary.transform,260,22','            resultState=Label(summary.transform,260,22')
summary=summary.replace('            Label(summary.transform,304,136','            resultDuration=Label(summary.transform,304,136')
summary=summary.replace('            ScrollCopy(observation.transform','            resultObservation=ScrollCopy(observation.transform')
summary=summary.replace('var widths=new float[5];','var widths=new float[5];resultAreas=new ResultArea[5];')
summary=summary.replace('                var button=row.gameObject.AddComponent<Button>();button.targetGraphic=row;button.onClick.AddListener(()=>SetResultTab(shift,1));','                var button=row.gameObject.AddComponent<Button>();button.targetGraphic=row;Wire(button,DispatchAction.ResultTab,1);')
summary=summary.replace('                Glyph(row.transform,10,6,32,(HudSymbol)i,tone);','                Glyph(row.transform,10,6,32,(HudSymbol)i,tone);\n                var areaIcon=row.GetComponentInChildren<HudGlyph>();')
summary=summary.replace('                Label(row.transform,65','                var areaName=Label(row.transform,65')
summary=summary.replace('                Label(row.transform,1280','                var areaPoints=Label(row.transform,1280')
summary=summary.replace('true).alignment=TextAnchor.MiddleRight;','true);areaPoints.alignment=TextAnchor.MiddleRight;\n                resultAreas[i]=new ResultArea{row=row,fill=fill,icon=areaIcon,name=areaName,points=areaPoints};')
summary=summary.replace('            BuildNotes(summaryRoot,24,636,724,result.positiveNotes,true);','            AuthorNotes(summaryRoot,24,true);').replace('            BuildNotes(summaryRoot,772,636,724,result.improvementNotes,false);','            AuthorNotes(summaryRoot,772,false);')
summary=summary.replace('            Label(footer.transform,80,8','            resultSource=Label(footer.transform,80,8').replace('            Label(footer.transform,80,38','            resultMath=Label(footer.transform,80,38')
tail='''
            resultRetry=Button(panel,24,950,724,60,"REINTENTAR CASO",null,new Color(.025f,.16f,.26f),25);
            Glyph(resultRetry.transform,28,12,36,HudSymbol.Retry,CyanAccent);
            resultNext=Button(panel,772,950,724,60,"CONTINUAR AL SIGUIENTE CASO",null,new Color(.025f,.28f,.15f),25);
            Glyph(resultNext.transform,26,13,34,HudSymbol.Play,Green);
            resultBack=Button(panel,24,950,1472,60,"VOLVER AL HISTORIAL",null,PanelHeader,25);
            resultBack.gameObject.SetActive(false);resultReport.SetActive(false);resultRoot.gameObject.SetActive(false);
        }
        private void AuthorNotes(Transform parent,float x,bool positive)
        {
            float w=724;Color tone=positive?Green:Amber;
            var card=Angular(parent,x,636,w,206,positive?new Color(.018f,.072f,.055f):new Color(.06f,.052f,.025f),tone,positive?"Aciertos":"Oportunidades");
            Angular(card.transform,0,0,w,44,tone,tone,"Cinta");
            Glyph(card.transform,20,7,31,positive?HudSymbol.Check:HudSymbol.Bars,Navy);
            Label(card.transform,74,0,w-94,44,positive?"LO HICISTE BIEN":"A MEJORAR",28,Navy,true);
            var empty=Label(card.transform,28,72,w-56,76,positive?"Todavía no hay aciertos registrados. Revisá el informe y volvé a intentarlo.":"No se registraron puntos pendientes en esta evaluación.",21,Color.white);
            var labels=new Text[3];var rows=new GameObject[3];
            for(int i=0;i<3;i++)
            {
                var row=Rect(card.transform,0,50+i*49,w,49,"Nota "+(i+1));rows[i]=row.gameObject;
                Glyph(row,22,10,26,positive?HudSymbol.Check:HudSymbol.Alert,tone);
                labels[i]=Label(row,64,0,w-90,45,"",19,Color.white);
                if(i<2)Box(row,64,46,w-90,1,new Color(tone.r,tone.g,tone.b,.24f));
            }
            if(positive){positiveLabels=labels;positiveRows=rows;positiveEmpty=empty;}
            else{improvementLabels=labels;improvementRows=rows;improvementEmpty=empty;}
        }
'''
p.write_text('''#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace EmergencyDispatch
{
    public partial class DispatchView
    {
        private void AuthorResults()
        {
            bool shift=false;int specificCaseNum=1;
            var result=new CallResult{grade="Caso resuelto",resource="AMB-04",observation="Observación del supervisor"};
'''+head+report+summary+tail+'    }\n}\n#endif\n',encoding='utf-8')
