using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace EmergencyDispatch
{
    public partial class DispatchView
    {
        [Serializable] private class ResultArea
        {
            public HudShape row, fill;
            public HudGlyph icon;
            public Text name, points;
            public float fullWidth=825;
        }
        [Header("Saved audit screen")]
        [SerializeField] private RectTransform resultRoot;
        [SerializeField] private GameObject resultSummary, resultReport;
        [SerializeField] private Text resultHeader, resultCase, resultScore, resultState, resultGrade, resultDuration;
        [SerializeField] private Text resultObservation, resultSource, resultMath, reportCopy;
        [SerializeField] private Image resultRing;
        [SerializeField] private ResultArea[] resultAreas;
        [SerializeField] private Text[] positiveLabels, improvementLabels;
        [SerializeField] private GameObject[] positiveRows, improvementRows;
        [SerializeField] private Text positiveEmpty, improvementEmpty;
        [SerializeField] private Button resultRetry, resultNext, resultBack;
        [SerializeField] private CanvasGroup resultGroup;
        private bool showingGuard;
        private Coroutine resultAnimation;
        private CallResult animatedResult;
        private int animatedScore = -1;
        private bool animatedPending;
        private bool animatedShift;

        private void CloseResult()
        {
            if(resultAnimation!=null) { StopCoroutine(resultAnimation);resultAnimation=null; }
            if(resultRoot!=null)resultRoot.gameObject.SetActive(false);
            resultPanel=null;
        }
        private void SetResultTab(bool shift,int tab)
        {
            resultTab=tab;BuildResult(shift,currentViewingResult,currentViewingBackAction,currentViewingCaseNum);
        }
        private void BuildResult(bool shift,CallResult specificResult=null,Action onBackAction=null,int specificCaseNum=-1)
        {
            var result=shift&&specificResult==null?DispatchAudit.Aggregate(game.Results):specificResult??game.Session?.Result;
            if(result==null)return;
            currentViewingResult=specificResult;currentViewingBackAction=onBackAction;currentViewingCaseNum=specificCaseNum;showingGuard=shift;
            if(resultAnimation!=null) { StopCoroutine(resultAnimation);resultAnimation=null; }
            resultRoot.gameObject.SetActive(true);resultRoot.SetAsLastSibling();resultPanel=resultRoot;
            resultGroup.alpha=1;resultRoot.localScale=Vector3.one;
            resultSummary.SetActive(resultTab==0);resultReport.SetActive(resultTab==1);
            resultHeader.text=game.ExamMode?"EXAMEN · "+(game.ExamTimedOut?"TIEMPO CUMPLIDO":"NIVEL FINALIZADO"):shift?"INFORME DE GUARDIA":"AUDITORÍA DE DESPACHO CAD";
            resultCase.text=shift?"":"CASO #"+(specificCaseNum>0?specificCaseNum:game.CaseNumber).ToString("00");
            bool archived=onBackAction!=null;
            resultBack.gameObject.SetActive(archived);resultRetry.gameObject.SetActive(!archived);resultNext.gameObject.SetActive(!archived);
            resultRetry.GetComponentInChildren<Text>().text=shift?"REVISAR INFORME":"REINTENTAR CASO";
            resultNext.GetComponentInChildren<Text>().text=game.ExamMode?"VOLVER AL MENÚ":shift?"NUEVA GUARDIA":game.RemainingCalls==0?"VER RESUMEN DE GUARDIA":"ELEGIR SIGUIENTE LLAMADA";
            if(resultTab==1) { SetScrollableText(reportCopy,ReportText(result),742);return; }
            Color gradeColor=result.Total>=75?Green:result.Total>=60?Amber:Red;
            resultRing.color=gradeColor;resultScore.text=result.Total.ToString();resultGrade.text=result.grade;resultGrade.color=gradeColor;
            resultState.text=result.evaluationPending?"RESULTADO PROVISIONAL · REVISIÓN IA":"RESULTADO FINAL";
            resultDuration.text=(int)(result.seconds/60)+"m "+((int)result.seconds%60).ToString("00")+"s"+(shift?" en guardia":" en línea  |  Unidad: "+result.resource);
            if(game.ExamMode) resultDuration.text=IncomingCallBoard.Duration(game.ExamElapsed)+" / 03:00 de nivel  |  Unidad: "+result.resource;
            SetScrollableText(resultObservation,result.observation,110);
            var values=DispatchAudit.Values(result);var fills=new RectTransform[5];var widths=new float[5];
            for(int i=0;i<5;i++)
            {
                var area=resultAreas[i];float ratio=(float)values[i]/DispatchAudit.Maxima[i];bool low=ratio<.6f;
                Color tone=low?new Color(1,.33f,.37f):CyanAccent;
                area.row.color=low?new Color(.13f,.035f,.054f):new Color(.025f,.065f,.105f);
                area.row.edge=low?Red:new Color(.04f,.20f,.31f);area.row.SetVerticesDirty();
                area.fill.color=area.fill.edge=tone;area.icon.color=tone;area.name.color=low?tone:Color.white;
                area.points.text=values[i]+" / "+DispatchAudit.Maxima[i];area.points.color=tone;
                fills[i]=area.fill.rectTransform;widths[i]=area.fullWidth*ratio;
            }
            PresentNotes(result.positiveNotes,positiveLabels,positiveRows,positiveEmpty);
            PresentNotes(result.improvementNotes,improvementLabels,improvementRows,improvementEmpty);
            resultSource.text=result.evaluationSource.ToUpperInvariant();
            resultMath.text=string.Join(" + ",values.Select(v=>v.ToString()))+" = "+result.Total+" / 100  ·  "+result.turns+" intervenciones analizadas";
            var identity=shift&&specificResult==null?game.Results.LastOrDefault():result;
            bool changed=animatedResult!=identity || animatedShift!=shift || animatedScore!=result.Total || animatedPending!=result.evaluationPending;
            resultScore.rectTransform.localScale=Vector3.one;
            if(changed)
            {
                animatedResult=identity;animatedShift=shift;animatedScore=result.Total;animatedPending=result.evaluationPending;
                resultAnimation=StartCoroutine(AnimateEntrance(resultGroup,resultRoot,result.Total,resultRing,resultScore,fills,widths));
            }
            else
            {
                resultRing.fillAmount=result.Total/100f;
                for(int i=0;i<fills.Length;i++)fills[i].sizeDelta=new Vector2(widths[i],fills[i].sizeDelta.y);
            }
        }
        private static void SetScrollableText(Text label,string text,float minimum)
        {
            label.text=text;var size=label.rectTransform.sizeDelta;size.y=Mathf.Max(minimum,label.preferredHeight+12);label.rectTransform.sizeDelta=size;
            label.rectTransform.anchoredPosition=Vector2.zero;
            label.GetComponentInParent<ScrollRect>().verticalNormalizedPosition=1;
        }
        private static void PresentNotes(List<string> notes,Text[] labels,GameObject[] rows,Text empty)
        {
            empty.gameObject.SetActive(notes.Count==0);
            for(int i=0;i<labels.Length;i++)
            {
                rows[i].SetActive(i<notes.Count);if(i>=notes.Count)continue;
                string preview=notes[i];
                if(preview.Length>130) { int sentence=preview.IndexOf(". ",StringComparison.Ordinal);if(sentence>0&&sentence<130)preview=preview.Substring(0,sentence+1)+" Detalle en el informe."; }
                labels[i].text=preview;
            }
        }
        private static string ReportText(CallResult result)
        {
            var copy=new StringBuilder();
            copy.AppendLine(result.Total+" / 100 · "+result.evaluationSource+(result.evaluationPending?" · Revisión IA pendiente":""));
            copy.AppendLine(result.turns+" intervenciones · "+result.operatorCharacters+" caracteres del operador. La extensión no suma puntos.\n");
            if(result.areas!=null)foreach(var area in result.areas)
            {
                if(area==null)continue;
                copy.AppendLine(area.name.ToUpperInvariant()+"  "+area.score+" / "+area.maximum);copy.AppendLine(area.explanation);
                if(area.name=="Comunicación"&&result.communicationReview!=null)foreach(var criterion in result.communicationReview)
                {
                    int index=Array.IndexOf(DispatchAudit.CriterionIds,criterion.id);
                    copy.AppendLine("\n"+(index<0?criterion.id:DispatchAudit.CriterionNames[index])+" · "+criterion.score+"/5: "+criterion.reason);
                    copy.AppendLine(string.IsNullOrWhiteSpace(criterion.evidence)?"Evidencia: conducta no registrada.":"OPERADOR: «"+criterion.evidence+"»");
                }
                copy.AppendLine();
            }
            copy.AppendLine("TRANSCRIPCIÓN COMPLETA\n");
            copy.AppendLine(result.transcriptLog.Count>0?string.Join("\n\n",result.transcriptLog):"No se registraron intervenciones.");
            return copy.ToString();
        }
    }
}
