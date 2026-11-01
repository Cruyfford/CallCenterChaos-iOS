using UnityEngine;
using System.Collections.Generic;

// ORIGINAL, modular, stylized 3D staff. Replaces the former identical capsule people.
// Staff are distinct in body proportions, outfits, faces, hairstyle and accessories.
public static class StaffStudio
{
    private struct Design
    {
        public Color skin, hair, shirt, pants, trim;
        public float height, breadth, headWidth;
        public int hairType, clothes, glasses, facialHair;
        public bool earrings;
        public Design(Color sk, Color ha, Color sh, Color pa, Color tr, float he, float br, float hw, int ht, int cl, int gl, int fh, bool e)
        { skin=sk;hair=ha;shirt=sh;pants=pa;trim=tr;height=he;breadth=br;headWidth=hw;hairType=ht;clothes=cl;glasses=gl;facialHair=fh;earrings=e; }
    }

    private static readonly Design[] staff = new Design[] {
        // Proportions and accessory combos differ intentionally.
        new Design(new Color(.78f,.45f,.29f),new Color(.12f,.09f,.10f),new Color(.17f,.65f,.58f),new Color(.13f,.19f,.27f),new Color(.91f,.76f,.35f),1.05f,1.00f,.92f,0,0,1,0,false),
        new Design(new Color(.91f,.65f,.48f),new Color(.57f,.34f,.17f),new Color(.91f,.40f,.23f),new Color(.21f,.21f,.33f),new Color(.96f,.90f,.74f),.94f,.86f,1.06f,1,1,0,1,true),
        new Design(new Color(.39f,.24f,.19f),new Color(.13f,.12f,.16f),new Color(.54f,.45f,.84f),new Color(.16f,.24f,.22f),new Color(.80f,.70f,.48f),1.12f,1.13f,1.07f,2,2,2,2,false),
        new Design(new Color(.97f,.78f,.61f),new Color(.70f,.47f,.20f),new Color(.84f,.64f,.27f),new Color(.25f,.33f,.36f),new Color(.40f,.26f,.18f),.99f,.94f,.94f,3,3,0,0,true),
        new Design(new Color(.62f,.38f,.25f),new Color(.20f,.17f,.13f),new Color(.42f,.58f,.81f),new Color(.25f,.20f,.27f),new Color(.96f,.85f,.70f),1.09f,.99f,1.15f,4,4,1,3,false),
        new Design(new Color(.83f,.51f,.34f),new Color(.14f,.13f,.13f),new Color(.63f,.18f,.19f),new Color(.19f,.16f,.20f),new Color(.96f,.79f,.36f),1.16f,1.19f,.96f,5,5,2,2,false)
    };

    private static readonly Dictionary<Color32,Material> mats = new Dictionary<Color32,Material>();
    private static Shader surfaceShader;

    private static Material Mat(Color col)
    {
        Color32 k = (Color32)col;
        Material m;
        if (mats.TryGetValue(k,out m)) return m;
        if (surfaceShader == null)
        {
            surfaceShader = Shader.Find("Universal Render Pipeline/Lit");
            if (surfaceShader == null) surfaceShader = Shader.Find("Standard");
            if (surfaceShader == null) surfaceShader = Shader.Find("Legacy Shaders/Diffuse");
        }
        m = new Material(surfaceShader);
        m.color = col;
        mats[k] = m;
        return m;
    }

    private static Transform Part(Transform parent, PrimitiveType shape, string name, Vector3 pos, Vector3 scale, Color col, Vector3 euler)
    {
        GameObject g = GameObject.CreatePrimitive(shape);
        g.name = name;
        Transform t = g.transform;
        t.SetParent(parent,false);
        t.localPosition=pos; t.localScale=scale; t.localEulerAngles=euler;
        var collider=g.GetComponent<Collider>();
        if (collider != null) Object.Destroy(collider);
        g.GetComponent<Renderer>().sharedMaterial=Mat(col);
        return t;
    }
    private static Transform Sphere(Transform p,string n,Vector3 xyz,Vector3 sc,Color c) {return Part(p,PrimitiveType.Sphere,n,xyz,sc,c,Vector3.zero);}
    private static Transform Box(Transform p,string n,Vector3 xyz,Vector3 sc,Color c) {return Part(p,PrimitiveType.Cube,n,xyz,sc,c,Vector3.zero);}
    private static Transform Cylinder(Transform p,string n,Vector3 xyz,Vector3 sc,Color c,Vector3 e) {return Part(p,PrimitiveType.Cylinder,n,xyz,sc,c,e);}
    private static Color Dark(Color c,float mul) {return new Color(c.r*mul,c.g*mul,c.b*mul,1f);}

    public static GameObject Build(Vector3 position, int index, bool manager=false)
    {
        var d=staff[(manager?5:index)%staff.Length];
        bool female=(index==1 || index==3 || index==4) && !manager;
        bool elder=(index==2 || index==4 || manager);
        if (female) d.facialHair=0;
        if (elder) { d.hair = index%2==0 ? new Color(.78f,.77f,.74f) : new Color(.62f,.64f,.69f); d.glasses=2; }
        GameObject root=new GameObject(manager?"Manager - distinct stylized model":"Coworker " + (index+1) + " - distinct stylized model");
        root.transform.position=position;
        root.transform.localScale=Vector3.one * d.height;
        root.transform.rotation=Quaternion.Euler(0,index%2==0?7:-12,0);

        Color shoe=Dark(d.pants,.58f),skinShade=Dark(d.skin,.87f);
        float b=d.breadth,h=d.headWidth;
        // Two legs, individual shoes, belt, pockets
        foreach(int side in new int[]{-1,1})
        {
            float x=side*.145f*b;
            Sphere(root.transform,"Tailored trouser leg",new Vector3(x,.40f,0),new Vector3(.25f,.75f,.28f),d.pants);
            Sphere(root.transform,"Shoe",new Vector3(x,.11f,-.095f),new Vector3(.30f,.20f,.43f),shoe);
            Box(root.transform,"Shoe toe accent",new Vector3(x,.086f,-.279f),new Vector3(.21f,.025f,.055f),d.trim);
        }
        Sphere(root.transform,"Torso shaped jacket",new Vector3(0,1.08f,0),new Vector3(.78f*b,.91f,.44f*b),d.shirt);
        Box(root.transform,"Belt",new Vector3(0,.72f,-.15f),new Vector3(.56f*b,.095f,.30f),shoe);
        Box(root.transform,"Buckle",new Vector3(0,.72f,-.32f),new Vector3(.095f,.08f,.035f),d.trim);
        // Different collars / tie / trim
        if (d.clothes==0 || d.clothes==4)
        {
            Box(root.transform,"Sweater collar",new Vector3(0,1.47f,-.13f),new Vector3(.28f,.09f,.22f),d.trim);
            if (d.clothes==4) for(int y=0;y<3;y++)Sphere(root.transform,"Cardigan button",new Vector3(0,1.36f-y*.13f,-.225f),new Vector3(.052f,.052f,.027f),d.trim);
        }
        else if (d.clothes==1 || d.clothes==5)
        {
            Sphere(root.transform,"Formal collar",new Vector3(0,1.5f,-.1f),new Vector3(.35f,.13f,.23f),d.trim);
            Box(root.transform,"Tie upper",new Vector3(0,1.40f,-.235f),new Vector3(.12f,.14f,.045f),new Color(.22f,.22f,.28f));
            Box(root.transform,"Tie lower",new Vector3(0,1.24f,-.248f),new Vector3(.09f,.21f,.045f),d.trim);
        }
        else if (d.clothes==2)
        {
            Sphere(root.transform,"Scarf",new Vector3(0,1.47f,0),new Vector3(.43f,.18f,.34f),d.trim);
            Box(root.transform,"Scarf end",new Vector3(.19f,1.29f,-.23f),new Vector3(.15f,.37f,.06f),d.trim);
        }
        else
        {
            Sphere(root.transform,"Hood trim",new Vector3(0,1.45f,.12f),new Vector3(.53f,.23f,.31f),Dark(d.shirt,.65f));
            foreach (int side in new[]{-1,1})
                Cylinder(root.transform,"Hood string",new Vector3(.09f*side,1.31f,-.23f),new Vector3(.018f,.15f,.018f),d.trim,Vector3.zero);
        }
        Box(root.transform,"Staff badge",new Vector3(.23f*b,1.23f,-.218f),new Vector3(.18f,.12f,.035f),new Color(.97f,.93f,.77f));
        Box(root.transform,"Badge stripe",new Vector3(.23f*b,1.22f,-.239f),new Vector3(.13f,.028f,.008f),d.trim);
        // Moving arm pivots are isolated for character gestures.
        Transform[] arms=new Transform[2];
        foreach (int side in new[]{-1,1})
        {
            var arm =new GameObject(side==-1?"Left arm animated":"Right arm animated").transform;
            arm.SetParent(root.transform,false);
            arm.localPosition=new Vector3(side*.42f*b,1.36f,.018f);
            Sphere(arm,"Sleeve",new Vector3(side*.11f,-.19f,0),new Vector3(.29f*b,.55f,.34f*b),d.shirt);
            Sphere(arm,"Hand",new Vector3(side*.18f,-.46f,-.04f),new Vector3(.19f,.22f,.18f),d.skin);
            Sphere(arm,"Sleeve cuff",new Vector3(side*.18f,-.37f,0),new Vector3(.24f,.10f,.24f),d.trim);
            arms[side==-1?0:1]=arm;
        }
        // Head pivot + fitted face, ears and articulated hairstyle
        var head=new GameObject("Head animated").transform;
        head.SetParent(root.transform,false);head.localPosition=new Vector3(0,1.74f,0);
        Sphere(head,"Neck",new Vector3(0,-.15f,.01f),new Vector3(.24f,.32f,.25f),skinShade);
        Sphere(head,"Face - independent proportions",new Vector3(0,.15f,-.01f),new Vector3(.55f*h,.64f,.53f),d.skin);
        Sphere(head,"Jaw volume",new Vector3(0,-.115f,-.075f),new Vector3(.39f*h,.27f,.40f),d.skin);
        if (elder)
        {
            Color crease=Dark(d.skin,.73f);
            for(int side=-1;side<=1;side+=2)
            {
                Box(head,"Senior smile line",new Vector3(side*.25f,-.08f,-.41f),new Vector3(.018f,.16f,.016f),crease);
                Box(head,"Senior eye crease",new Vector3(side*.25f,.14f,-.43f),new Vector3(.13f,.016f,.016f),crease);
            }
            Box(head,"Forehead wrinkle",new Vector3(0,.34f,-.45f),new Vector3(.26f,.015f,.012f),crease);
        }
        foreach (int side in new[]{-1,1})
        {
            Sphere(head,"Ear",new Vector3(side*.30f*h,.10f,0),new Vector3(.13f,.20f,.16f),d.skin);
            Sphere(head,"Eye white",new Vector3(side*.125f,.17f,-.48f),new Vector3(.125f,.105f,.047f),new Color(.98f,.97f,.94f));
            Sphere(head,"Iris",new Vector3(side*.125f,.16f,-.523f),new Vector3(.065f,.073f,.020f),d.facialHair==1?new Color(.27f,.40f,.35f):new Color(.28f,.20f,.18f));
            Sphere(head,"Pupil",new Vector3(side*.125f,.16f,-.541f),new Vector3(.028f,.044f,.014f),new Color(.05f,.04f,.05f));
            Box(head,"Eyebrow",new Vector3(side*.13f,.30f,-.479f),new Vector3(.18f,.045f,.045f),d.hair);
            if(d.earrings && side==-1)Sphere(head,"Earring",new Vector3(-.34f*h,-.04f,-.09f),new Vector3(.055f,.075f,.055f),d.trim);
        }
        Sphere(head,"Nose",new Vector3(0,.055f,-.515f),new Vector3(.10f,.17f,.17f),skinShade);
        Box(head,"Mouth line",new Vector3(0,-.13f,-.483f),new Vector3(.20f,.024f,.026f),Dark(d.skin,.38f));
        if (d.facialHair>0)
        {
            if(d.facialHair==1)
                Sphere(head,"Goatee",new Vector3(0,-.28f,-.31f),new Vector3(.15f,.16f,.10f),d.hair);
            else
            {
                Sphere(head,"Beard chin",new Vector3(0,-.295f,-.27f),new Vector3(.38f*h,.21f,.25f),d.hair);
                Sphere(head,"Mustache",new Vector3(0,-.085f,-.512f),new Vector3(.24f,.075f,.052f),d.hair);
            }
        }
        // Six genuinely different sculpted hairstyles; no wigs cloned by recoloring.
        if(d.hairType==0)
        {
            Sphere(head,"Side part top",new Vector3(-.07f,.49f,.02f),new Vector3(.60f*h,.25f,.54f),d.hair);
            Sphere(head,"Swept fringe",new Vector3(-.13f,.42f,-.32f),new Vector3(.40f,.14f,.22f),d.hair);
        }
        else if(d.hairType==1)
        {
            Sphere(head,"Round curls cap",new Vector3(0,.47f,.0f),new Vector3(.64f*h,.29f,.60f),d.hair);
            for(int k=0;k<8;k++)
            {
                float t=k*Mathf.PI*.27f;
                Sphere(head,"Individual curl",new Vector3(Mathf.Sin(t)*.27f,.52f+Mathf.Cos(t*2)*.05f,-.17f),new Vector3(.21f,.21f,.21f),d.hair);
            }
        }
        else if(d.hairType==2)
        {
            Sphere(head,"Buzz fade hair",new Vector3(0,.48f,0),new Vector3(.57f*h,.14f,.54f),d.hair);
            Sphere(head,"Hair crest",new Vector3(0,.53f,-.16f),new Vector3(.37f,.19f,.35f),Dark(d.hair,1.15f));
        }
        else if(d.hairType==3)
        {
            Sphere(head,"Tied top hairstyle",new Vector3(0,.44f,.015f),new Vector3(.58f*h,.19f,.56f),d.hair);
            Sphere(head,"Hair bun",new Vector3(0,.57f,.29f),new Vector3(.27f,.27f,.28f),d.hair);
            Sphere(head,"Side strand",new Vector3(-.29f*h,.18f,-.16f),new Vector3(.10f,.43f,.11f),d.hair);
        }
        else if(d.hairType==4)
        {
            Sphere(head,"Long parted hair",new Vector3(0,.43f,.1f),new Vector3(.65f*h,.24f,.60f),d.hair);
            Sphere(head,"Long left lock",new Vector3(-.31f*h,.09f,.03f),new Vector3(.17f,.65f,.23f),d.hair);
            Sphere(head,"Long right lock",new Vector3(.31f*h,.09f,.03f),new Vector3(.17f,.65f,.23f),d.hair);
        }
        else
        {
            Sphere(head,"Manager slicked-back hair",new Vector3(0,.48f,.12f),new Vector3(.62f*h,.18f,.54f),d.hair);
            for(int k=0;k<4;k++)Box(head,"Hair part detail",new Vector3(-.19f+k*.12f,.56f,-.12f),new Vector3(.027f,.03f,.28f),Dark(d.hair,1.37f));
        }
        if(d.glasses>0)
        {
            Color rims=d.glasses==2?d.trim:new Color(.09f,.12f,.14f);
            foreach(int side in new[]{-1,1})
            {
                Sphere(head,"Glasses lens rim",new Vector3(side*.125f,.16f,-.508f),new Vector3(.24f,.19f,.04f),rims);
                Sphere(head,"Glasses transparent-look lens",new Vector3(side*.125f,.16f,-.535f),new Vector3(.195f,.15f,.025f),new Color(.78f,.89f,.88f));
            }
            Box(head,"Glasses bridge",new Vector3(0,.16f,-.544f),new Vector3(.095f,.037f,.04f),rims);
        }
        if (female && index==4)
        {
            // Long hair locks distinguish the older female office coworker.
            Sphere(head,"Silver hair side lock L",new Vector3(-.31f,.01f,.13f),new Vector3(.15f,.71f,.2f),d.hair);
            Sphere(head,"Silver hair side lock R",new Vector3(.31f,.01f,.13f),new Vector3(.15f,.71f,.2f),d.hair);
        }
        // Real headset assembly: overhead arc approximated with lightweight segmented curved parts.
        Color headphone =new Color(.095f,.11f,.16f);
        for(int k=0;k<7;k++)
        {
            float theta=(k/6f)*Mathf.PI;
            float xx=Mathf.Cos(theta)*.35f*h;
            float yy=.18f+Mathf.Sin(theta)*.59f;
            Sphere(head,"Headset overhead curved band",new Vector3(xx,yy,.055f),new Vector3(.087f,.10f,.09f),headphone);
        }
        foreach(int side in new[]{-1,1})
        {
            Sphere(head,"Headset cup",new Vector3(side*.35f*h,.13f,.005f),new Vector3(.14f,.30f,.26f),headphone);
            Sphere(head,"Headset ear pad",new Vector3(side*.38f*h,.13f,-.01f),new Vector3(.09f,.22f,.19f),d.trim);
        }
        Box(head,"Mic arm stem",new Vector3(-.27f,-.058f,-.30f),new Vector3(.045f,.035f,.40f),headphone);
        Sphere(head,"Microphone capsule",new Vector3(-.13f,-.055f,-.50f),new Vector3(.12f,.07f,.09f),headphone);

        var anim =root.AddComponent<StaffIdleMotion>();
        anim.head=head;anim.leftArm=arms[0];anim.rightArm=arms[1];anim.offset=index*.82f;
        return root;
    }
}

public sealed class StaffIdleMotion : MonoBehaviour
{
    public Transform head,leftArm,rightArm;
    public float offset;
    void Update()
    {
        float t=Time.time*.65f+offset;
        if(head!=null)head.localRotation=Quaternion.Euler(Mathf.Sin(t)*2.3f,Mathf.Sin(t*.67f)*5f,0);
        if(leftArm!=null)leftArm.localRotation=Quaternion.Euler(Mathf.Sin(t*.74f)*8f,0,-13f+Mathf.Sin(t)*5f);
        if(rightArm!=null)rightArm.localRotation=Quaternion.Euler(Mathf.Sin(t*.81f+2)*8f,0,13f+Mathf.Sin(t+.8f)*5f);
    }
}
