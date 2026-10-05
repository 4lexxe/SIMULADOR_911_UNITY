using System;
using UnityEngine;
using UnityEngine.UI;

namespace EmergencyDispatch
{
    public sealed class HistoryRowView : MonoBehaviour
    {
        public Text caseLabel, score, grade, title, meta, observation;
        public Image gradeBackground;
        public Button open;
        private Action<CallResult,int> show;
        private CallResult result;
        private int caseNumber;
        public void Bind(CallResult value,int number,string caseTitle,Action<CallResult,int> onOpen)
        {
            result=value;caseNumber=number;show=onOpen;
            caseLabel.text="CASO #"+number.ToString("00");score.text=result.Total+" pts";
            Color tone=result.Total>=80?DispatchView.Green:result.Total>=65?DispatchView.CyanAccent:result.Total>=50?DispatchView.Amber:DispatchView.Red;
            grade.text=result.Total>=90?"A":result.Total>=75?"B":result.Total>=60?"C":"D";grade.color=tone;
            gradeBackground.color=new Color(tone.r*.2f,tone.g*.2f,tone.b*.2f,.9f);
            title.text=caseTitle;meta.text=(int)(result.seconds/60)+"m "+(int)(result.seconds%60)+"s en línea · Recurso: "+result.resource;
            observation.text=string.IsNullOrEmpty(result.observation)?"Sin observaciones del supervisor.":result.observation;
            open.onClick.AddListener(Open);
        }
        private void Open() { EmergencyAudioManager.Instance?.PlayButtonClick();show?.Invoke(result,caseNumber); }
        private void OnDestroy() { if(open!=null)open.onClick.RemoveListener(Open); }
    }
}
