using System;
using System.Collections.Generic;
using UnityEngine;

// An ORIGINAL, procedural call-center satire prototype. No third-party game assets are included.
// Add this to an empty GameObject in the scene; its world and UI are built at runtime.
public class OfficePrototype : MonoBehaviour
{
    private enum ScreenPage { World, Desktop, Phone, Directory, Script, RemotePC, Paint, Camera, Review, AISettings }
    private ScreenPage page = ScreenPage.World;
    private CharacterController player;
    private Camera playerCamera;
    private Transform playerRoot;
    private float pitch, yaw;
    private Vector2 moveAxis;
    private int joystickFinger = -1, lookFinger = -1;
    private Vector2 joystickOrigin, joystickCurrent, lookPrev;
    private const float RunSeconds = 240f;
    private float timeLeft = RunSeconds;
    private int day = 1, earned = 0, quota = 600;
    private int trust, callCount;
    private bool callActive, shiftFinished, virusActive;
    private string caller = "", callerLine = "", eventMessage = "مرحبًا بك في أول يوم عمل!";
    private CallerRoster roster;
    private AIChatClient aiClient;
    private string sessionId = "";
    private bool aiPending;
    private string aiStatus = "بدون إنترنت / حوار محلي";
    private string aiServerInput = "";
    private string aiSettingsMessage = "أدخل رابط HTTPS لخادم الذكاء الاصطناعي الخاص باللعبة.";
    private string playerTypedLine = "";
    private int aiRequestVersion;

    private CallerProfile activeProfile;
    private int directoryPage = 0;
    private int directoryFilter = 0;  // 0 all, 1 women, 2 men, 3 ages 60+
    private string spokenText = "";
    private string customerReply = "";
    private readonly string[] events = {
        "The printer launched 200 blank pages!", "Office lights are flickering!", "The boss demands more productivity!",
        "A fake computer virus is spreading!", "A suspiciously loud coffee machine explodes!"
    };
    private Texture2D white;
    private GUIStyle headerStyle, bodyStyle, smallStyle, buttonStyle, miniButtonStyle, panelStyle, darkBoxStyle;
    private float eventClock;
    private Color brushColor = new Color(.95f,.39f,.18f);
    private readonly List<Vector2> paintPoints = new List<Vector2>();
    private float nextClickTime;

    private void Awake()
    {
        Application.targetFrameRate = 60;
        roster = new CallerRoster();
        aiClient = gameObject.AddComponent<AIChatClient>();
        aiServerInput = AIChatClient.BackendUrl;
        VoiceBridge.SetLanguage(PlayerPrefs.GetString("ccc.voice.locale", "ar-SA"));

#if UNITY_IOS && !UNITY_EDITOR
        Screen.orientation = ScreenOrientation.LandscapeLeft;
#endif
        BuildOffice();
        white = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        white.SetPixel(0, 0, Color.white);
        white.Apply();
    }

    private void Update()
    {
        if (page == ScreenPage.World) MovePlayer();
        if (!shiftFinished)
        {
            timeLeft -= Time.deltaTime;
            eventClock += Time.deltaTime;
            if (timeLeft <= 0) ReviewDay();
            if (eventClock > 47f)
            {
                eventClock = 0;
                eventMessage = events[UnityEngine.Random.Range(0, events.Length)];
                virusActive = UnityEngine.Random.value < .35f;
            }
        }
    }

    private void MovePlayer()
    {
        if (player == null || playerCamera == null) return;
        moveAxis = Vector2.zero;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) moveAxis.y += 1;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) moveAxis.y -= 1;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) moveAxis.x -= 1;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) moveAxis.x += 1;
        if (Input.GetKeyDown(KeyCode.E)) page = ScreenPage.Desktop;
        if (Input.GetKeyDown(KeyCode.Escape)) page = ScreenPage.Desktop;

        foreach (Touch touch in Input.touches)
        {
            // Touch.position has a bottom-left origin; keep controls in the bottom of the screen.
            if (touch.phase == TouchPhase.Began)
            {
                if (touch.position.x < Screen.width * .34f && touch.position.y < Screen.height * .50f && joystickFinger < 0)
                { joystickFinger = touch.fingerId; joystickOrigin = touch.position; joystickCurrent = touch.position; }
                else if (touch.position.x >= Screen.width * .34f && touch.position.y < Screen.height * .65f && lookFinger < 0)
                { lookFinger = touch.fingerId; lookPrev = touch.position; }
            }
            if (touch.fingerId == joystickFinger)
            {
                joystickCurrent = touch.position;
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) joystickFinger = -1;
            }
            if (touch.fingerId == lookFinger)
            {
                if (touch.phase == TouchPhase.Moved)
                {
                    Vector2 delta = touch.position - lookPrev;
                    yaw += delta.x * .11f;
                    pitch = Mathf.Clamp(pitch - delta.y * .11f, -72, 72);
                    lookPrev = touch.position;
                }
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) lookFinger = -1;
            }
        }
        if (joystickFinger >= 0)
        {
            Vector2 touchDir = Vector2.ClampMagnitude((joystickCurrent - joystickOrigin) / (Screen.height * .13f), 1f);
            moveAxis = touchDir;
        }
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButton(1))
        {
            yaw += Input.GetAxis("Mouse X") * 3f;
            pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * 3f, -72, 72);
        }
#endif
        playerRoot.rotation = Quaternion.Euler(0, yaw, 0);
        playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
        Vector3 direction = playerRoot.forward * moveAxis.y + playerRoot.right * moveAxis.x;
        if (direction.sqrMagnitude > 1) direction.Normalize();
        player.Move((direction * 3.5f + Vector3.down * 2f) * Time.deltaTime);
    }

    private void BuildOffice()
    {
        foreach (Camera c in FindObjectsOfType<Camera>()) c.enabled = false;
        Color cream = new Color(.85f, .76f, .65f), tan = new Color(.58f, .45f, .38f);
        MakeCube("Tiled office floor", new Vector3(0, -.18f, 0), new Vector3(16, .36f, 14), new Color(.39f, .35f, .33f));
        MakeCube("North wall", new Vector3(0, 2.7f, 7), new Vector3(16, 5.4f, .25f), cream);
        MakeCube("South wall", new Vector3(0, 2.7f, -7), new Vector3(16, 5.4f, .25f), cream);
        MakeCube("West wall", new Vector3(-8, 2.7f, 0), new Vector3(.25f, 5.4f, 14), cream);
        MakeCube("East wall", new Vector3(8, 2.7f, 0), new Vector3(.25f, 5.4f, 14), cream);
        for (int a = -5; a <= 5; a += 5)
        {
            for (int b = -3; b <= 3; b += 6)
            {
                MakeCube("Ceiling light", new Vector3(a, 4.8f, b), new Vector3(2.9f, .08f, .26f), new Color(1, .95f, .78f));
                var lamp = new GameObject("Office lamp").AddComponent<Light>();
                lamp.type = LightType.Point; lamp.intensity = 2.2f; lamp.range = 9; lamp.transform.position = new Vector3(a, 4.4f, b);
            }
        }
        int staffIndex=0;
        for (int row = 0; row < 2; row++)
        for (int col = 0; col < 3; col++)
        {
            float x = -4.4f + col * 4.4f, z = -1f + row * 4.2f;
            MakeCube("Office desk", new Vector3(x, .75f, z), new Vector3(2.65f, .18f, 1.4f), tan);
            MakeCube("Desk leg left", new Vector3(x-1.1f, .35f, z), new Vector3(.12f, .7f, 1.15f), new Color(.24f, .23f, .25f));
            MakeCube("Desk leg right", new Vector3(x+1.1f, .35f, z), new Vector3(.12f, .7f, 1.15f), new Color(.24f, .23f, .25f));
            MakeCube("Monitor", new Vector3(x, 1.25f, z+.35f), new Vector3(.95f, .65f, .09f), new Color(.06f,.09f,.12f));
            MakeCube("Monitor image", new Vector3(x, 1.25f, z+.285f), new Vector3(.86f, .56f, .025f), new Color(.12f,.47f,.37f));
            MakeCube("Keyboard", new Vector3(x, .86f, z-.25f), new Vector3(.85f, .035f, .25f), new Color(.13f,.14f,.15f));
            MakeCube("Cubicle partition", new Vector3(x+1.48f, 1.25f, z), new Vector3(.08f, 1.45f, 1.8f), new Color(.41f, .35f, .42f));
            MakeCube("Chair", new Vector3(x, .5f, z-1.08f), new Vector3(.85f, .25f, .75f), new Color(.17f,.17f,.22f));
            MakeCube("Chair back", new Vector3(x, .94f, z-1.45f), new Vector3(.85f, .85f, .18f), new Color(.18f,.19f,.26f));
            if (!(row == 0 && col == 1)) StaffStudio.Build(new Vector3(x, 0, z-2f), staffIndex++);
        }
        MakeCube("Whiteboard", new Vector3(0, 2.5f, 6.81f), new Vector3(4.2f, 1.8f, .15f), new Color(.94f,.93f,.82f));
        MakeCube("Boss desk", new Vector3(-5.5f,.7f,5.5f), new Vector3(2.5f,.2f,1.3f), new Color(.36f,.16f,.16f));
        StaffStudio.Build(new Vector3(-5.6f, 0, 4.4f), 5, true);
        MakeCube("Emergency door", new Vector3(6.5f,1.7f,6.82f), new Vector3(1.8f,3.4f,.14f), new Color(.35f,.48f,.45f));
        MakeCube("Coffee machine", new Vector3(6.7f,.8f,-5.7f), new Vector3(.8f,1.6f,.65f), new Color(.17f,.2f,.21f));
        RenderSettings.ambientLight = new Color(.67f, .65f, .58f);
        var sun = new GameObject("Warm ambient light").AddComponent<Light>();
        sun.type = LightType.Directional; sun.intensity = .5f; sun.transform.rotation = Quaternion.Euler(55, -30, 0);
        var p = new GameObject("Local player");
        playerRoot = p.transform; p.transform.position = new Vector3(0, .06f, -4.25f);
        player = p.AddComponent<CharacterController>(); player.height = 1.75f; player.radius = .3f; player.center = new Vector3(0, .87f, 0);
        var eye = new GameObject("First person view"); eye.transform.SetParent(p.transform, false); eye.transform.localPosition = new Vector3(0, 1.6f, 0);
        playerCamera = eye.AddComponent<Camera>(); playerCamera.fieldOfView = 75; playerCamera.nearClipPlane = .03f; eye.tag = "MainCamera";
        eye.AddComponent<AudioListener>();
    }

    private Material MakeMaterial(Color color)
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit");
        if (s == null) s = Shader.Find("Standard");
        if (s == null) s = Shader.Find("Sprites/Default");
        Material m = new Material(s); m.color = color; return m;
    }
    private GameObject MakeCube(string title, Vector3 at, Vector3 scale, Color color)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = title;
        g.transform.position = at; g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = MakeMaterial(color); return g;
    }
    private GameObject MakeSphere(string title, Vector3 at, Vector3 scale, Color color)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere); g.name = title;
        g.transform.position = at; g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = MakeMaterial(color); return g;
    }
    private void NewCall()
    {
        BeginCall(roster == null ? null : roster.GetRandom());
    }

    private void BeginCall(CallerProfile selected)
    {
        if (selected == null)
        {
            eventMessage = "بيانات الشخصيات غير موجودة. شغّل مولّد الشخصيات.";
            page = ScreenPage.Phone;
            return;
        }
        callCount++;
        // Every call gets a fresh ID; old responses cannot update the new call.
        sessionId = Guid.NewGuid().ToString("N");
        aiRequestVersion++;
        aiPending = false;
        aiStatus = string.IsNullOrEmpty(AIChatClient.BackendUrl) ? "بدون إنترنت / حوار محلي" : "خادم الذكاء الاصطناعي جاهز للاتصال";
        activeProfile = selected;
        caller = selected.displayName;
        callerLine = selected.greeting;
        spokenText = "";
        customerReply = callerLine;
        trust = Mathf.Clamp(selected.patience, 27, 65);
        callActive = true;
        VoiceBridge.Stop();
        VoiceBridge.SetVoice(selected.voiceVariant, selected.voicePitch, selected.voiceRate, selected.gender, selected.language);
        VoiceBridge.Speak(customerReply);
        page = ScreenPage.Phone;
    }

    private void Choice(int choice)
    {
        if (!callActive || aiPending || Time.time < nextClickTime) return;
        nextClickTime = Time.time + .25f;
        int delta = choice == 0 ? 13 : choice == 1 ? 9 : -14;
        if (virusActive && choice == 2) delta -= 5;
        string text = choice == 0 ? "أقدر أساعدك في حل المشكلة الغريبة." :
                      choice == 1 ? "خلنا نفحص المشكلة خطوة بخطوة." : "اسرع خلص المكالمة الآن!";
        SubmitPlayerLine(text, delta);
    }

    private void SubmitPlayerLine(string text, int fallbackTrust)
    {
        if (!callActive || aiPending || string.IsNullOrWhiteSpace(text)) return;
        spokenText = text.Trim();
        if (string.IsNullOrEmpty(AIChatClient.BackendUrl))
        {
            OfflineReply(fallbackTrust);
            return;
        }
        aiPending = true;
        aiStatus = "العميل يفكر في الرد...";
        customerReply = "...";
        int version = ++aiRequestVersion;
        string callId = sessionId;
        aiClient.Send(callId, activeProfile.id, spokenText, result =>
        {
            if (this == null || !callActive || callId != sessionId || version != aiRequestVersion) return;
            aiPending = false;
            if (result.ok)
            {
                customerReply = result.reply;
                trust = Mathf.Clamp(trust + Mathf.Clamp(result.trustDelta, -20, 20), 0, 100);
                aiStatus = result.mode == "mock" ? "تجربة فقط / ذكاء اصطناعي وهمي" : "ذكاء اصطناعي مباشر / " + result.mood;
                VoiceBridge.Speak(customerReply);
                if (trust < 15) { eventMessage = caller + " hung up."; EndCallWithoutReward(); }
            }
            else
            {
                aiStatus = "تعذر الاتصال / رد محلي";
                Debug.LogWarning("AI service fallback: " + result.error);
                OfflineReply(fallbackTrust);
            }
        });
    }

    private void OfflineReply(int delta)
    {
        trust = Mathf.Clamp(trust + delta + UnityEngine.Random.Range(-3,4), 0, 100);
        customerReply = activeProfile == null ? "ممكن توضح لي أكثر؟" :
                        trust > 75 ? activeProfile.highTrustReply :
                        trust < 25 ? activeProfile.lowTrustReply : activeProfile.mediumTrustReply;
        VoiceBridge.Speak(customerReply);
        if (trust < 15) { eventMessage = caller + " hung up."; EndCallWithoutReward(); }
    }

    private void EndCallWithoutReward()
    {
        aiRequestVersion++;
        aiPending = false;
        callActive = false;
        VoiceBridge.Stop();
    }

    private void EndCall()
    {
        if (!callActive) return;
        int amount = trust >= 70 ? UnityEngine.Random.Range(100,221) : trust >= 45 ? UnityEngine.Random.Range(30,91) : 0;
        earned += amount;
        eventMessage = amount > 0 ? caller + " gave your imaginary team " + amount + " game credits!" : caller + " did not earn credits.";
        EndCallWithoutReward();
    }
    private void ReviewDay()
    {
        shiftFinished = true; EndCallWithoutReward(); timeLeft = 0;
        page = ScreenPage.Review;
    }
    private void NextDay()
    {
        if (earned < quota) return;
        day++;
        earned = 0; quota += 220; timeLeft = RunSeconds; shiftFinished = false;
        virusActive = false; eventClock = 0; eventMessage = "Another day in this bizarre office.";
        page = ScreenPage.World;
    }
    public void OnVoiceRecognized(string recognizedText)
    {
        // Callback from iOS native speech recognition (one final transcription only).
        if (!callActive || aiPending || string.IsNullOrWhiteSpace(recognizedText)) return;
        SubmitPlayerLine(recognizedText, recognizedText.Length > 12 ? 12 : 5);
    }

    private void InitStyles()
    {
        if (headerStyle != null) return;
        headerStyle = new GUIStyle(GUI.skin.label) { fontSize=29, fontStyle=FontStyle.Bold, normal={ textColor=Color.white } };
        bodyStyle = new GUIStyle(GUI.skin.label) { fontSize=23, wordWrap=true, normal={ textColor=new Color(.93f,.96f,.94f) } };
        smallStyle = new GUIStyle(GUI.skin.label) { fontSize=18, wordWrap=true, normal={ textColor=new Color(.85f,.86f,.89f) } };
        buttonStyle = new GUIStyle(GUI.skin.button) { fontSize=23, fontStyle=FontStyle.Bold, wordWrap=true, padding=new RectOffset(12,12,8,8), normal={ textColor=Color.white } };
        buttonStyle.normal.background = Solid(new Color(.14f,.34f,.37f));
        buttonStyle.hover.background = Solid(new Color(.21f,.44f,.46f));
        buttonStyle.active.background = Solid(new Color(.13f,.54f,.48f));
        miniButtonStyle = new GUIStyle(buttonStyle) { fontSize=16, wordWrap=false, padding=new RectOffset(4,4,4,4) };
        panelStyle = new GUIStyle(GUI.skin.box); panelStyle.normal.background = Solid(new Color(.08f,.12f,.16f,.97f));
        darkBoxStyle = new GUIStyle(GUI.skin.box); darkBoxStyle.normal.background = Solid(new Color(.1f,.13f,.2f,.8f));
    }
    private Texture2D Solid(Color c)
    {
        var t = new Texture2D(1,1); t.SetPixel(0,0,c); t.Apply(); return t;
    }
    private void Label(Rect rect, string text, bool header=false) => GUI.Label(rect,text,header?headerStyle:bodyStyle);
    private bool Btn(Rect rect, string text) => GUI.Button(rect,text,buttonStyle);

    private void OnGUI()
    {
        InitStyles();
        GUI.matrix = Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(Screen.width/1600f,Screen.height/900f,1));
        DrawRect(new Rect(0,0,1600,72), new Color(.03f,.06f,.08f,.85f));
        Label(new Rect(28,13,490,52), "CALL CENTER CHAOS  |  DAY " + day, true);
        Label(new Rect(610,16,470,50), "CREDITS: " + earned + " / " + quota);
        Label(new Rect(1110,16,435,50), "TIME: " + Mathf.CeilToInt(timeLeft) + "s");
        switch(page)
        {
            case ScreenPage.World: DrawWorldGUI(); break;
            case ScreenPage.Desktop: DrawDesktop(); break;
            case ScreenPage.Phone: DrawPhone(); break;
            case ScreenPage.Directory: DrawDirectory(); break;
            case ScreenPage.Script: DrawScript(); break;
            case ScreenPage.RemotePC: DrawRemotePC(); break;
            case ScreenPage.Paint: DrawPaint(); break;
            case ScreenPage.Camera: DrawCamera(); break;
            case ScreenPage.Review: DrawReview(); break;
            case ScreenPage.AISettings: DrawAISettings(); break;
        }
    }
    private void DrawRect(Rect rect,Color color)
    {
        Color prior=GUI.color; GUI.color=color;GUI.DrawTexture(rect,white);GUI.color=prior;
    }
    private void Panel(string title)
    {
        GUI.Box(new Rect(165,100,1270,735),GUIContent.none,panelStyle);
        Label(new Rect(205,129,1100,48),title,true);
        if(Btn(new Rect(1270,117,130,55),"CLOSE")) page = ScreenPage.World;
    }
    private void DrawWorldGUI()
    {
        GUI.Box(new Rect(25,98,560,102), GUIContent.none,darkBoxStyle);
        GUI.Label(new Rect(42,113,520,79), eventMessage,smallStyle);
        if(Btn(new Rect(1260,106,295,75),"فتح الكمبيوتر")) page = ScreenPage.Desktop;
        GUI.Box(new Rect(60,650,265,195),GUIContent.none,darkBoxStyle);
        Label(new Rect(95,670,230,44),"الحركة",true);
        Label(new Rect(100,738,190,68),"اسحب للمشي",false);
        GUI.Box(new Rect(1250,646,290,199),GUIContent.none,darkBoxStyle);
        Label(new Rect(1294,674,205,44),"النظر",true);
        Label(new Rect(1272,738,260,70),"اسحب للدوران",false);
        if(Btn(new Rect(605,780,345,74),"المكتب / الكمبيوتر")) page = ScreenPage.Desktop;
        // A minimal crosshair, useful for the first-person view.
        DrawRect(new Rect(797,437,6,24),new Color(1,1,1,.6f));
        DrawRect(new Rect(788,446,24,6),new Color(1,1,1,.6f));
    }
    private void DrawDesktop()
    {
        Panel("نظام المكتب | شاشة الكمبيوتر");
        GUI.Label(new Rect(205,196,1050,55),"جميع المكالمات والأموال في هذه اللعبة خيالية.",smallStyle);
        string[] items = {"الهاتف", "الدليل", "الكمبيوتر", "الرسم", "الكاميرا", "تقييم المدير", "144 شخصية", "الذكاء الاصطناعي"};
        for(int i=0;i<items.Length;i++)
        {
            int row=i/4,col=i%4;
            if(Btn(new Rect(228+col*294,286+row*190,254,128),items[i]))
            {
                if(i==0) page=ScreenPage.Phone;
                else if(i==1) page=ScreenPage.Script;
                else if(i==2) page=ScreenPage.RemotePC;
                else if(i==3) page=ScreenPage.Paint;
                else if(i==4) page=ScreenPage.Camera;
                else if(i==5) ReviewDay();
                else if(i == 6) { directoryPage = 0; page = ScreenPage.Directory; }
                else { aiServerInput = AIChatClient.BackendUrl; page = ScreenPage.AISettings; }
            }
        }
        GUI.Label(new Rect(205,744,1165,65),eventMessage,smallStyle);
    }
    private void DrawPortrait(CallerProfile profile, Rect rect)
    {
        Texture2D portrait = roster == null ? null : roster.Portrait(profile);
        if (portrait != null)
        {
            GUI.DrawTexture(rect, portrait, ScaleMode.ScaleToFit);
        }
        else
        {
            DrawRect(rect, new Color(.17f,.23f,.28f));
            GUI.Label(rect, "?", headerStyle);
        }
    }

    private void DrawPhone()
    {
        Panel("الهاتف | مكالمات خيالية بالذكاء الاصطناعي");
        if(!callActive)
        {
            Label(new Rect(225,200,1000,52),"لا توجد مكالمة حاليًا", true);
            Label(new Rect(226,260,1090,54),"144 شخصية متنوعة بين نساء ورجال وكبار سن، لكل شخصية طباع وصوت.");
            if(Btn(new Rect(370,344,840,95),"مكالمة عشوائية")) NewCall();
            if(Btn(new Rect(370,467,840,95),"استعراض 144 شخصية")) { directoryPage=0; page=ScreenPage.Directory; }
            if(Btn(new Rect(555,650,470,78),"العودة للكمبيوتر")) page=ScreenPage.Desktop;
            return;
        }
        DrawPortrait(activeProfile,new Rect(219,198,217,217));
        Label(new Rect(476,199,806,52),caller, true);
        GUI.Label(new Rect(476,253,802,72),
            activeProfile == null ? "متصل جديد" :
            activeProfile.personality+" | "+activeProfile.occupation+" | "+activeProfile.age+" سنة",smallStyle);
        Label(new Rect(477,328,700,45),"ثقة المتصل");
        DrawRect(new Rect(477,385,690,28),new Color(.22f,.24f,.28f));
        DrawRect(new Rect(477,385,690*trust/100f,28),trust>=65?new Color(.18f,.76f,.49f):new Color(.94f,.60f,.22f));
        Label(new Rect(1203,370,120,60),trust+"%",true);
        GUI.Box(new Rect(210,447,1140,114),GUIContent.none,darkBoxStyle);
        GUI.Label(new Rect(230,463,1098,72),customerReply,bodyStyle);
        GUI.Label(new Rect(230,542,1080,27),aiStatus,smallStyle);
        if(Btn(new Rect(221,583,356,74),"تحدث بلطف")) Choice(0);
        if(Btn(new Rect(597,583,356,74),"اسأل عن المشكلة")) Choice(1);
        if(Btn(new Rect(973,583,356,74),"استعجل المتصل")) Choice(2);
        if(Btn(new Rect(221,673,353,66),aiPending ? "جارٍ التفكير..." : "تحدث (المايك)"))
        { if (!aiPending) VoiceBridge.Listen(gameObject.name); }
        if(Btn(new Rect(597,673,353,66),"إعادة الصوت")) VoiceBridge.Speak(customerReply);
        if(Btn(new Rect(973,673,353,66),"إنهاء المكالمة")) EndCall();
        GUI.Label(new Rect(223,749,370,48),"أنت: " + spokenText,smallStyle);
        playerTypedLine = GUI.TextField(new Rect(600,753,470,51),playerTypedLine,240);
        if(Btn(new Rect(1084,751,240,54),"إرسال") && !aiPending)
        {
            string toSend = playerTypedLine.Trim();
            playerTypedLine = "";
            SubmitPlayerLine(toSend, 8);
            GUI.FocusControl(null);
        }
    }

    private void DrawAISettings()
    {
        Panel("الذكاء الاصطناعي | محادثات مباشرة");
        GUI.Label(new Rect(226,207,1050,104),
            "استخدم خادم HTTPS آمنًا للعبة. لا تضع مفتاح الخدمة داخل تطبيق الآيفون. " +
            "كل واحدة من الشخصيات الـ144 لها طباع مستقلة وذاكرة محادثة منفصلة.",bodyStyle);
        Label(new Rect(226,343,950,55),"رابط خادم الذكاء الاصطناعي (HTTPS)");
        aiServerInput = GUI.TextField(new Rect(226,409,1080,64),aiServerInput,250);
        if(Btn(new Rect(226,515,330,74),"حفظ الخادم"))
        {
            string error;
            if(AIChatClient.SetBackendUrl(aiServerInput,out error))
                aiSettingsMessage = string.IsNullOrEmpty(AIChatClient.BackendUrl) ? "تم إيقاف AI؛ الحوارات المحلية متاحة." : "تم الحفظ. اختبر الاتصال الآن.";
            else aiSettingsMessage = error;
        }
        if(Btn(new Rect(585,515,360,74),"اختبار الاتصال"))
        {
            aiSettingsMessage = "جارٍ اختبار الاتصال...";
            aiClient.CheckHealth((ok, message) => { if(this != null) aiSettingsMessage = message; });
        }
        if(Btn(new Rect(977,515,330,74),"إيقاف AI"))
        {
            string ignored;
            AIChatClient.SetBackendUrl("",out ignored);
            aiServerInput = "";
            aiSettingsMessage = "تم إيقاف AI؛ الحوارات المحلية متاحة.";
        }
        GUI.Label(new Rect(226,609,1050,56),aiSettingsMessage,bodyStyle);
        Label(new Rect(226,694,450,50),"لغة الميكروفون:");
        if(Btn(new Rect(635,678,235,65),"إنجليزي")) { VoiceBridge.SetLanguage("en-US"); aiSettingsMessage="لغة المايك: إنجليزي"; }
        if(Btn(new Rect(884,678,218,65),"عربي")) { VoiceBridge.SetLanguage("ar-SA"); aiSettingsMessage="لغة المايك: عربي"; }
        if(Btn(new Rect(1110,751,198,58),"الكمبيوتر")) page=ScreenPage.Desktop;
    }

    private void DrawDirectory()
    {
        Panel("الشخصيات | " + (roster == null ? 0 : roster.Count) + " متصل مختلف");
        if (roster == null || roster.Count == 0)
        {
            Label(new Rect(235,280,1040,80),"لم يتم العثور على صور. شغّل أداة توليد الشخصيات.");
            return;
        }
        var indices = new List<int>();
        for (int i=0;i<roster.Count;i++)
        {
            CallerProfile p=roster.Get(i);
            if (directoryFilter==0 || (directoryFilter==1 && p.gender=="female") ||
                (directoryFilter==2 && p.gender=="male") || (directoryFilter==3 && p.age>=60))
                indices.Add(i);
        }
        string[] filters = {"الكل (144)", "نساء (72)", "رجال (72)", "كبار السن (48)"};
        for(int k=0;k<filters.Length;k++)
        {
            int filter=k;
            if(Btn(new Rect(220+k*293,188,272,44),filters[k]) && filter!=directoryFilter)
            { directoryFilter=filter; directoryPage=0; }
        }
        const int itemsPerPage=18;
        int totalPages = Mathf.Max(1,(indices.Count+itemsPerPage-1) / itemsPerPage);
        directoryPage=Mathf.Clamp(directoryPage,0,totalPages-1);
        for (int j=0;j<itemsPerPage;j++)
        {
            int offset=directoryPage*itemsPerPage+j;
            if(offset>=indices.Count) break;
            int idx=indices[offset];
            CallerProfile info=roster.Get(idx);
            int col=j%6,row=j/6;
            float x=228+col*195, y=238+row*164;
            GUI.Box(new Rect(x-7,y-7,179,153),GUIContent.none,darkBoxStyle);
            DrawPortrait(info,new Rect(x,y,105,105));
            GUI.Label(new Rect(x+111,y+2,62,24),"#"+(idx+1),smallStyle);
            GUI.Label(new Rect(x+111,y+28,64,75),info.age+" سنة",smallStyle);
            if(GUI.Button(new Rect(x,y+108,166,37),info.displayName,miniButtonStyle)) BeginCall(info);
        }
        if(Btn(new Rect(240,757,275,60),"السابق") && directoryPage>0) directoryPage--;
        Label(new Rect(664,759,290,57),"صفحة "+(directoryPage+1)+" / "+totalPages);
        if(Btn(new Rect(1044,757,275,60),"التالي") && directoryPage<totalPages-1) directoryPage++;
    }

    private void DrawScript()
    {
        Panel("دليل المساعدة | مكتب الاتصالات");
        GUI.Label(new Rect(225,224,1090,430),
            "أهلًا بك في مركز الاتصالات الخيالي!\n\n"+
            "1. أجب على مكالمات الشخصيات الوهمية.\n\n"+
            "2. تحدث بلطف أو اسأل أو استعجل.\n\n"+
            "3. ارفع ثقة المتصل وأكمل المهمة الخيالية.\n\n"+
            "4. اربح أرصدة وهمية لتحقيق هدف اليوم.\n\n"+
            "5. تابع أحداث المكتب وتقييم المدير.\n\n"+
            "لا توجد مدفوعات أو بيانات بنكية أو ضحايا حقيقيون.",bodyStyle);
        if(Btn(new Rect(1070,705,245,67),"الكمبيوتر"))page=ScreenPage.Desktop;
    }
    private int passPuzzle = 0;
    private void DrawRemotePC()
    {
        Panel("الكمبيوتر البعيد | لعبة مصغرة خيالية");
        Label(new Rect(225,216,1110,92),"هناك أربع دوائر إلكترونية معطلة؛ أصلحها للحصول على أرصدة افتراضية.");
        for(int i=0;i<4;i++)
        {
            bool lit = i<passPuzzle;
            DrawRect(new Rect(280+i*270,370,204,100),lit?new Color(.2f,.7f,.4f):new Color(.45f,.20f,.19f));
            Label(new Rect(335+i*270,397,170,50),lit?"تعمل":"معطلة");
        }
        if(Btn(new Rect(510,556,565,90),"أصلح الدائرة التالية"))
        {
            passPuzzle++;
            if(passPuzzle>=4) {earned+=60;eventMessage="Circuit puzzle complete: +60 pretend credits";passPuzzle=0;}
        }
        if(Btn(new Rect(1060,700,275,77),"الكمبيوتر"))page=ScreenPage.Desktop;
    }
    private void DrawPaint()
    {
        Panel("الرسم | رسومات المكتب");
        Rect canvas=new Rect(247,220,1080,430);
        DrawRect(canvas,new Color(.96f,.94f,.84f));
        foreach(Vector2 point in paintPoints) DrawRect(new Rect(point.x-7,point.y-7,14,14),brushColor);
        Event e=Event.current;
        if((e.type==EventType.MouseDrag || e.type==EventType.MouseDown) && canvas.Contains(e.mousePosition) && paintPoints.Count<4500)
        { paintPoints.Add(e.mousePosition); e.Use(); }
        if(Btn(new Rect(290,696,290,67),"مسح الرسم"))paintPoints.Clear();
        if(Btn(new Rect(1000,696,290,67),"الكمبيوتر"))page=ScreenPage.Desktop;
    }
    private void DrawCamera()
    {
        Panel("كاميرا المكتب");
        GUI.Label(new Rect(250,260,1095,200),"هذه معاينة من كاميرا اللاعب ثلاثية الأبعاد داخل المكتب.",bodyStyle);
        if(Btn(new Rect(505,590,560,91),"العودة للمكتب"))page=ScreenPage.World;
    }
    private void DrawReview()
    {
        Panel("تقييم المدير في نهاية اليوم");
        Label(new Rect(330,245,980,68),"اليوم " + day + " - الأداء",true);
        Label(new Rect(330,345,950,66),"الأرباح: " + earned + " / المطلوب: " + quota);
        bool success=earned>=quota;
        Label(new Rect(330,445,950,80),success?"حققت الهدف! ممتاز!":"لم تحقق الهدف. المدير غاضب!",true);
        if(success && Btn(new Rect(390,620,765,96),"ابدأ اليوم التالي"))NextDay();
        if(!success && Btn(new Rect(390,620,765,96),"أعد المحاولة"))
        { earned=0;timeLeft=RunSeconds;shiftFinished=false;eventClock=0;page=ScreenPage.World; }
    }
    private void OnDestroy()
    {
        VoiceBridge.Stop();
        if(white!=null)Destroy(white);
    }
}
