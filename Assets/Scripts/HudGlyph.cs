using UnityEngine;
using UnityEngine.UI;

namespace EmergencyDispatch
{
    public enum HudSymbol { Target, Chat, Tactic, Clock, File, Check, Alert, Play, Retry, Bars, Mic, Headset, Person, Folder, Settings, Team, Exit, Chevron, Edit }

    // A small drawn icon set, independent of emoji or operating-system fonts.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HudGlyph : MaskableGraphic
    {
        public HudSymbol symbol;
        private VertexHelper mesh;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); mesh = vh;
            switch (symbol)
            {
                case HudSymbol.Headset:
                    for(int i=0;i<24;i++)Arc(.5f,.48f,.28f,180+i*7.5f,180+(i+1)*7.5f);
                    for(int i=0;i<19;i++)Arc(.5f,.48f,.43f,110+i*12,110+(i+1)*12);
                    Line(.22f,.45f,.22f,.65f,.12f);Line(.78f,.45f,.78f,.65f,.12f);
                    Line(.75f,.65f,.67f,.81f);Line(.67f,.81f,.52f,.81f);Ring(.48f,.81f,.035f);break;
                case HudSymbol.Person:
                    Ring(.5f,.3f,.16f);
                    for(int i=0;i<20;i++)Arc(.5f,.79f,.27f,180+i*9,180+(i+1)*9);
                    Line(.23f,.79f,.77f,.79f);break;
                case HudSymbol.Folder:
                    Line(.12f,.26f,.38f,.26f);Line(.38f,.26f,.49f,.39f);Line(.49f,.39f,.88f,.39f);
                    Line(.12f,.26f,.12f,.8f);Line(.12f,.8f,.8f,.8f);Line(.8f,.8f,.92f,.5f);Line(.92f,.5f,.25f,.5f);Line(.25f,.5f,.12f,.8f);break;
                case HudSymbol.Settings:
                    Ring(.5f,.5f,.24f);Ring(.5f,.5f,.08f);
                    for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Line(.5f+Mathf.Cos(a)*.25f,.5f+Mathf.Sin(a)*.25f,.5f+Mathf.Cos(a)*.39f,.5f+Mathf.Sin(a)*.39f,.1f);}break;
                case HudSymbol.Team:
                    Ring(.5f,.25f,.12f);Ring(.2f,.38f,.09f);Ring(.8f,.38f,.09f);
                    Line(.35f,.55f,.35f,.82f,.13f);Line(.65f,.55f,.65f,.82f,.13f);Line(.35f,.55f,.65f,.55f);
                    Line(.14f,.58f,.14f,.8f,.11f);Line(.86f,.58f,.86f,.8f,.11f);break;
                case HudSymbol.Exit:
                    Line(.5f,.16f,.2f,.16f);Line(.2f,.16f,.2f,.84f);Line(.2f,.84f,.5f,.84f);
                    Line(.43f,.5f,.87f,.5f);Line(.68f,.32f,.87f,.5f);Line(.87f,.5f,.68f,.68f);break;
                case HudSymbol.Chevron:Line(.32f,.2f,.64f,.5f,.1f);Line(.64f,.5f,.32f,.8f,.1f);break;
                case HudSymbol.Edit:Line(.22f,.74f,.75f,.21f,.14f);Line(.16f,.86f,.86f,.86f);break;
                case HudSymbol.Target:
                    Ring(.5f,.5f,.31f); Ring(.5f,.5f,.1f);
                    Line(.5f,.08f,.5f,.28f); Line(.5f,.72f,.5f,.92f); Line(.08f,.5f,.28f,.5f); Line(.72f,.5f,.92f,.5f); break;
                case HudSymbol.Chat:
                    Line(.15f,.18f,.85f,.18f); Line(.85f,.18f,.85f,.67f); Line(.85f,.67f,.4f,.67f); Line(.4f,.67f,.18f,.85f); Line(.18f,.85f,.18f,.18f);
                    Line(.32f,.35f,.7f,.35f); Line(.32f,.49f,.59f,.49f); break;
                case HudSymbol.Tactic:
                    Line(.5f,.5f,.5f,.17f); Line(.5f,.5f,.2f,.76f); Line(.5f,.5f,.8f,.76f);
                    Ring(.5f,.15f,.11f); Ring(.18f,.79f,.11f); Ring(.82f,.79f,.11f); Ring(.5f,.48f,.12f); break;
                case HudSymbol.Clock:
                    Ring(.5f,.55f,.32f); Line(.4f,.08f,.6f,.08f); Line(.5f,.08f,.5f,.22f); Line(.5f,.36f,.5f,.55f); Line(.5f,.55f,.65f,.55f); break;
                case HudSymbol.File:
                    Line(.25f,.12f,.65f,.12f); Line(.65f,.12f,.82f,.3f); Line(.82f,.3f,.82f,.88f); Line(.82f,.88f,.25f,.88f); Line(.25f,.88f,.25f,.12f);
                    Line(.38f,.43f,.68f,.43f); Line(.38f,.58f,.68f,.58f); Line(.38f,.73f,.6f,.73f); break;
                case HudSymbol.Check: Line(.18f,.51f,.4f,.72f,.12f); Line(.4f,.72f,.84f,.25f,.12f); break;
                case HudSymbol.Alert:
                    Line(.5f,.1f,.08f,.87f); Line(.08f,.87f,.92f,.87f); Line(.92f,.87f,.5f,.1f); Line(.5f,.36f,.5f,.6f,.08f); Line(.5f,.72f,.5f,.76f,.08f); break;
                case HudSymbol.Play: Triangle(P(.25f,.14f),P(.85f,.5f),P(.25f,.86f)); break;
                case HudSymbol.Retry:
                    for (int i=0;i<23;i++) Arc(.5f,.5f,.32f, i*12+40, (i+1)*12+40);
                    Line(.77f,.15f,.77f,.41f,.08f); Line(.77f,.41f,.52f,.36f,.08f); break;
                case HudSymbol.Bars: Line(.22f,.63f,.22f,.86f,.14f); Line(.49f,.4f,.49f,.86f,.14f); Line(.76f,.15f,.76f,.86f,.14f); break;
                case HudSymbol.Mic:
                    Ring(.5f,.31f,.16f); Line(.25f,.38f,.25f,.55f); Line(.25f,.55f,.5f,.7f); Line(.5f,.7f,.75f,.55f); Line(.75f,.55f,.75f,.38f); Line(.5f,.7f,.5f,.86f); Line(.34f,.86f,.66f,.86f); break;
            }
        }
        private Vector2 P(float x,float y) { var r=rectTransform.rect; return new Vector2(r.x+x*r.width,r.yMax-y*r.height); }
        private void Triangle(Vector2 a,Vector2 b,Vector2 c) { int n=mesh.currentVertCount; mesh.AddVert(a,color,Vector2.zero); mesh.AddVert(b,color,Vector2.zero); mesh.AddVert(c,color,Vector2.zero); mesh.AddTriangle(n,n+1,n+2); }
        private void Line(float x,float y,float xx,float yy,float width=.055f)
        {
            Vector2 a=P(x,y),b=P(xx,yy),d=(b-a).normalized;
            Vector2 n=new Vector2(-d.y,d.x)*Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*width*.5f;
            Triangle(a+n,b+n,b-n); Triangle(a+n,b-n,a-n);
        }
        private void Arc(float x,float y,float radius,float a,float b)
        {
            a*=Mathf.Deg2Rad;b*=Mathf.Deg2Rad;float inner=Mathf.Max(0,radius-.0275f),outer=radius+.0275f;
            Vector2 aa=P(x+Mathf.Cos(a)*outer,y+Mathf.Sin(a)*outer),bb=P(x+Mathf.Cos(b)*outer,y+Mathf.Sin(b)*outer);
            Vector2 cc=P(x+Mathf.Cos(b)*inner,y+Mathf.Sin(b)*inner),dd=P(x+Mathf.Cos(a)*inner,y+Mathf.Sin(a)*inner);
            Triangle(aa,bb,cc);Triangle(aa,cc,dd);
        }
        private void Ring(float x,float y,float r) { for(int i=0;i<40;i++) Arc(x,y,r,i*9,(i+1)*9); }
    }
}
