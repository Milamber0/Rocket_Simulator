using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SimController : MonoBehaviour
{
    private Rocket m_Rocket;
    public static double m_OrbitTargetRadius = 6356766f;
    private static float startYRocket = 9009f;
    private double m_UnityScaleFix = (m_OrbitTargetRadius / startYRocket);
    public int m_TimeScale = 1000;
    private double m_FramePosition_x;
    private double m_FramePosition_y;
    private double m_SimulatedTime;
    private double m_Angle;
    private float m_ThrottleControl;
    private int numPredictSteps = 100000;
    public ParticleSystem m_rocketTrail;
    public Transform m_RocketVisual;
    public Camera m_VisualCamera;
    public GUISkin m_CustomMenuSkin;

    private bool m_OnStartScreen = true;
    private bool m_PauseToggle = false;

    private List<Vector3> m_PredictedPoints;
    private List<Vector3> m_PredictedPointsCopy;
    public DrawPrediction m_DrawPrediction;

    public Transform m_RocketMarker;
    public Transform m_Ground;

    // Gui elements
    public GUIText m_SpeedGUI;
    public GUIText m_AltitudeGUI;
    public GUIText m_TimeGUI;
    public GUIText m_TimeScaleGUI;
    public GUIText m_AccelerationGUI;
    public GUIText m_FuelGUI;
    public GUIText m_ApogeeGUI;
    public GUIText m_PerigeeGUI;
    public GUIText m_DragGUI;

    CrewComponent m_CrewComponent;
    FuelComponent m_FuelComponent;
    EngineComponent m_EngineComponent;
    float m_RocketDiameter;

    //User Inputs//
    string[] m_UserInputStrings = new string[12];
    float[] m_UserInputs = new float[12];

    public struct MenuEntry
    {
        public string label;
        public int column;

        public MenuEntry(string a_string, int a_int)
        {
            label = a_string;
            column = a_int;
        }
    }

    // Needs to be ordered ascending by column!
    static MenuEntry[] m_MenuEntries = new MenuEntry[12]
    {
       new MenuEntry("Crew Pod Weight in kg:",          0)
       ,new MenuEntry("Crew Pod drag:",                 0)
       ,new MenuEntry("Fuel Hull Weight in kg:",        1)
       ,new MenuEntry("Fuel Weight in kg:",             1)
       ,new MenuEntry("Fuel Tank drag:",                1)
       ,new MenuEntry("Engine Weight in kg:",           2)
       ,new MenuEntry("Specific Impulse Sea Level:",    2)
       ,new MenuEntry("Specific Impulse Vacuum:",       2)
       ,new MenuEntry("Fuel Consumption Sea Level:",    2)
       ,new MenuEntry("Fuel Consumption Vacuum:",       2)
       ,new MenuEntry("Engine drag:",                   2)
       ,new MenuEntry("Rocket Diameter in m:",          3)
    };


    const int MAX_TIMEINCREASE = 20000;
    const int MAX_USERINPUT = 10;


    private void Start()
    {
        for (int i = 0; i < m_UserInputStrings.Length; i++)
        {
            m_UserInputStrings[i] = "0";
            m_UserInputs[i] = 0.0f;
        }

        m_OnStartScreen = true;
    }

    private void Initialize()
    {
        // Setup our rocket, with fuel components and engines
        m_Rocket = new Rocket(m_RocketDiameter, 0.0, startYRocket * m_UnityScaleFix);

        m_Angle = 0.5 * Mathf.PI;   // Starting with a vertical launch.  // 0.41678 * Mathf.PI; // ~75 degree angle -> http://www.g2mil.com/fundamentals.htm
        m_ThrottleControl = 0.0f;//             // Starting with 0% throttle.
        m_rocketTrail.Play();                   // Make sure the trail effect is initiated.

        m_PredictedPoints = new List<Vector3>();

        m_Rocket.AddComponent(m_CrewComponent);
        m_Rocket.AddComponent(m_FuelComponent);
        m_Rocket.AddComponent(m_EngineComponent);
    }

    void Update()
    {
        // Run Controls
        CheckHotKeys();

        // Game loop

        if (!m_OnStartScreen)
        {
            Time.timeScale = (((m_TimeScale) < 1) || m_Rocket.HitGround()) ? 0 : 1;     //If time is turned down to 0 pause the game too.
	
	        if (m_Rocket.SetAngle(m_Angle) || (transform.eulerAngles != new Vector3(0, 0, ((float)m_Angle * Mathf.Rad2Deg) - 90.0f)))
	        {
	            transform.eulerAngles = new Vector3(0, 0, ((float)m_Angle * Mathf.Rad2Deg) - 90.0f);
	        }
	        m_Rocket.SetThrottle(m_ThrottleControl);
	
	        ParticleSystem.EmissionModule em = m_rocketTrail.emission;
	        if (m_ThrottleControl > 0.0f && m_Rocket.GetFuel() >= 0.1)
	        {
	            em.enabled = true;
	            em.rateOverTime = m_ThrottleControl * 500;
	        }
	        else
	        {
	            em.enabled = false;
	        }
	        
	        for (int i = 0; i < m_TimeScale; i++)
	        {
	            RunFrame();
	        }
	
	        transform.position = new Vector3((float)(m_FramePosition_x / m_UnityScaleFix), (float)(m_FramePosition_y / m_UnityScaleFix), 0.0f);
	        m_RocketVisual.rotation = transform.rotation;
	
	        ParticleSystem.CollisionModule cm = m_rocketTrail.collision;
	
	        //sky color
	        float rocketAltitude = m_Rocket.GetAltitude();
	
	        const float altitudeThermosphere = 84000.0f;
	        const float offset = 0.05f;
	        Color defaultColor = new Color(0, offset / 2, offset, 1);
	        if (rocketAltitude < altitudeThermosphere)
	        {
	            float newColorBlue = 1 - (m_Rocket.GetAltitude() / altitudeThermosphere);
	            float newColorGreen = newColorBlue / 2;
	            m_VisualCamera.backgroundColor = new Color(0, newColorGreen + offset / 2, newColorBlue + offset, 1);
	        }
	        else if (m_VisualCamera.backgroundColor != defaultColor)
	        {
	            m_VisualCamera.backgroundColor = defaultColor;
	        }
	
	        //Move visual ground and handle rocket trail effect collider.
	        if (rocketAltitude < 300 && rocketAltitude >= 0.0f)
	        {
	            m_Ground.GetComponent<Renderer>().enabled = true;
	            cm.enabled = true;
	            m_Ground.position = new Vector3(0.0f, (float)(9009 - ((m_Rocket.GetAltitude() + m_OrbitTargetRadius) / m_UnityScaleFix)), 2.0f);
	        }
	        else
	        {
	            m_Ground.GetComponent<Renderer>().enabled = false;
	            cm.enabled = false;
	        }
	
	        PredictPath();
        }
    }

    private void CheckHotKeys()
    {


        if (!m_OnStartScreen)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                Restart();
            }

            if (!m_Rocket.HitGround())
	        {
	            if (Input.GetKeyDown(KeyCode.X))
	            {
	                if (m_ThrottleControl <= 0.0f)
	                {
	                    m_ThrottleControl = 1.0f;
	                }
	                else
	                {
	                    m_ThrottleControl = 0.0f;
	                }
	            }
	
	            if (Input.GetKey(KeyCode.A))
	            {
	                m_Angle += (Mathf.PI * 0.01f);
	            }
	
	            if (Input.GetKey(KeyCode.D))
	            {
	                m_Angle += -(Mathf.PI * 0.01f);
	            }
	
	
	            if (Input.GetKey(KeyCode.W))
	            {
	                if (m_TimeScale + 1 <= MAX_TIMEINCREASE)
	                {
	                    m_TimeScale += 1;
                        m_PauseToggle = false;
	                }
	            }
	
	            if (Input.GetKey(KeyCode.S))
	            {
	                if (m_TimeScale - 1 >= 0)
	                {
	                    m_TimeScale -= 1;
	                }
                    if (m_TimeScale <= 0)
                    {
                        m_PauseToggle = true;
                    }
                }

                if (Input.GetKeyDown(KeyCode.P))
                {
                    if (m_PauseToggle == false)
                    {
                        m_TimeScale = 0;
                        m_PauseToggle = true;
                    }
                    else
                    {
                        m_TimeScale = 1;
                        m_PauseToggle = false;
                    }
                }

                if (Input.GetKey(KeyCode.T))
                {
                    m_TimeScale = 1;
                }

                if (Input.GetKey(KeyCode.Q))
	            {
	                m_ThrottleControl -= 0.01f;
	
	                if (m_ThrottleControl < 0.0f)
	                {
	                    m_ThrottleControl = 0.0f;
	                }
	            }
	
	            if (Input.GetKey(KeyCode.E))
	            {
	                m_ThrottleControl += 0.01f;
	
	                if (m_ThrottleControl > 1.0f)
	                {
	                    m_ThrottleControl = 1.0f;
	                }
	            }
	        }
        }
    }

    private void PredictPath()
    {
        if (m_ThrottleControl != 0.0f && m_Rocket.GetFuel() > 0.0f || m_Rocket.GetDrag() > 0.0001f)
        {
            m_PredictedPoints.Clear();

            // We want to see the predicted path at your current velocity, ignoring your acceleration. 
            // If we wanted to include the full model we could store the equation state and do RunFrame N more times, and restore it after the predictions.
            // Since that would be a very heavy calculation to run hundreds or thousands of predictions for, so we'll be using a simplified method with a fixed average deltaTime.
            // It won't be 100% accurate but it should be enough. 
            // Calculations based on a fluctuating deltaTime value will not give the same result every frame anyways, and the result would be horrible.

            // Run N number of frames ahead of time and log the new positions so we can draw the projected path
            Vector3 predictedPosition = m_Rocket.GetPosition();
            Vector3 predictedVelocity = m_Rocket.GetVelocityVector();
            bool hitPlanet = false;
            for (int i = 0; i < numPredictSteps; i++)
            {
                hitPlanet = m_Rocket.SimplifiedPredictNewPosition(predictedPosition, predictedVelocity, 0.07814091f, out predictedPosition, out predictedVelocity);

                if (hitPlanet)
                {
                    Debug.Log("HitPlanet at " + i);
                    break;
                }

                m_PredictedPoints.Add(predictedPosition / (float)m_UnityScaleFix);
            }
            m_PredictedPointsCopy = m_PredictedPoints;
            if (!hitPlanet)
            {
                Debug.Log("Missed Planet");
            }
        }
    }

    public List<Vector3> GetPredictedPoints()
    {
        return m_PredictedPointsCopy;
    }

    public int GetNumPredictSteps()
    {
        return numPredictSteps;
    }

    private float Wrap(float val, float min, float max)
    {
        val = val - (float)Mathf.Round((val - min) / (max - min)) * (max - min);
        if (val < 0)
            val = val + max - min;
        return val;
    }

    void OnGUI()
    {
        DrawControls();
        StartScreen();
        DrawHUD();
    }

    void DrawHUD()
    {
        System.TimeSpan timeSpan = System.TimeSpan.FromSeconds(m_SimulatedTime);
        string timeText = string.Format("{0:D2}:{1:D2}:{2:D2}:{3:D2}", timeSpan.Days, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);

        if (m_Rocket != null)
        {
            m_TimeScaleGUI.text =       "Time Scale: " + m_TimeScale;
            m_TimeGUI.text =            "Time: " + timeText;
            m_AltitudeGUI.text =        "Altitude: " + m_Rocket.GetAltitude().ToString("F2") + "m";
            m_SpeedGUI.text =           "Speed: " + m_Rocket.GetVelocity().ToString("F2") + "m/s";
            m_AccelerationGUI.text =    "Acceleration: " + m_Rocket.GetAcceleration().ToString("F2") + "m/s^2";
            m_FuelGUI.text =            "Fuel: " + m_Rocket.GetFuel().ToString("F2") + "kg";
            m_ApogeeGUI.text =          "Apogee Altitude: " + m_DrawPrediction.GetApogee().ToString("F0") + "m";
            m_PerigeeGUI.text =         "Perigee Altitude: " + m_DrawPrediction.GetPerigee().ToString("F0") + "m";
            m_DragGUI.text =            "Drag: " + m_Rocket.GetDrag().ToString("F4") + "N";

            if (m_Rocket.HitGround())
            {
                RestartScreen();
            }
        }
    }

    void DrawControls()
    {
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        m_ThrottleControl = GUI.VerticalSlider(new Rect(Screen.width / 4 - 5, Screen.height - 200, 10, 100), m_ThrottleControl, 1.0f, 0.0f);
        GUI.TextField(new Rect(Screen.width / 4 - 40, Screen.height - 260, 30, 0), "Throttle: " + (m_ThrottleControl * 100).ToString("F2") + "% \nX to toggle \nIncrease/Decrease:\nE/Q", style);

        double tempAngleDeg = (m_Angle * Mathf.Rad2Deg);
        double tempAngle = Wrap((float)tempAngleDeg, 1, 360);
        m_Angle = GUI.HorizontalSlider(new Rect(Screen.width / 4 - 120, Screen.height - 110, 100, 10), ((float)tempAngle * Mathf.Deg2Rad), 2.0f * Mathf.PI, 0.0f * Mathf.PI);
        GUI.TextField(new Rect(Screen.width / 4 - 140, Screen.height - 130, 30, 0), "Angle: " + (tempAngle).ToString("F2") + " Degrees\n\n\nRotate:\nW/S", style);


        m_TimeScale = (int)GUI.HorizontalSlider(new Rect(Screen.width / 4 + 20, Screen.height - 110, 100, 10), (float)m_TimeScale, 1.0F, 1000.0F);
        GUI.TextField(new Rect(Screen.width / 4 + 10, Screen.height - 130, 30, 0), "Time Scale: " + (m_TimeScale) + "\n\n\nIncrease/Decrease:\nW/S", style);
        if (GUI.Button(new Rect(Screen.width / 4 + 135, Screen.height - 130, 70, 25), "Faster"))
        {
            m_TimeScale += 50;
            if (m_TimeScale > MAX_TIMEINCREASE)
            {
                m_TimeScale = MAX_TIMEINCREASE;
            }
        }
        if (GUI.Button(new Rect(Screen.width / 4 + 205, Screen.height - 130, 20, 25), "+"))
        {
            m_TimeScale += 250;
            if (m_TimeScale > MAX_TIMEINCREASE)
            {
                m_TimeScale = MAX_TIMEINCREASE;
            }
        }
        if (GUI.Button(new Rect(Screen.width / 4 + 135, Screen.height - 105, 70, 25), "Slower"))
        {
            m_TimeScale -= 50;
            if (m_TimeScale <= 0)
            {
                m_TimeScale = 1;
            }
        }
        if (GUI.Button(new Rect(Screen.width / 4 + 205, Screen.height - 105, 20, 25), "-"))
        {
            m_TimeScale -= 250;
            if (m_TimeScale <= 0)
            {
                m_TimeScale = 1;
            }
        }
        if (GUI.Button(new Rect(Screen.width / 4 + 135, Screen.height - 80, 70, 25), "Reset T"))
        {
            m_TimeScale = 1;
        }
        if (GUI.Button(new Rect(Screen.width / 4 + 135, Screen.height - 55, 70, 25), "Pause"))
        {
            if (m_PauseToggle == false)
            {
                m_TimeScale = 0;
                m_PauseToggle = true;
            }
            else
            {
                m_TimeScale = 1;
                m_PauseToggle = false;
            }
        }

        if (GUI.Button(new Rect(Screen.width / 4 - 50, Screen.height - 30, 100, 25), "Rocket Select"))
        {
            HardRestart();
        }
    }

    void Restart()
    {
        m_SimulatedTime = 0;
        m_TimeScale = 1;

        if (m_PredictedPointsCopy != null)
        m_PredictedPointsCopy.Clear();

        m_rocketTrail.Clear();
        System.GC.Collect();
        Initialize();
    }

    void HardRestart()
    {
        System.GC.Collect();
        SceneManager.LoadScene(0);        
    }

    void RestartScreen()
    {
        GUIStyle style = new GUIStyle(GUI.skin.box);
        GUIStyle style2 = new GUIStyle();
        int buttonWidth = 100;
        int buttonHeight = 25;

        style.alignment = style2.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 50;
        style2.fontSize = style.fontSize / 2;
        style2.normal.textColor = style.normal.textColor;

        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "Game Over", style);
        GUI.Label(new Rect(0, 0 + style.fontSize * 1.5f, Screen.width, Screen.height), "Press R to quick restart", style2);

        if (GUI.Button(new Rect(Screen.width / 2 - buttonWidth, Screen.height / 2 + style.fontSize * 0.5f, buttonWidth, buttonHeight), "Restart"))
        {
            Restart();
        }

        if (GUI.Button(new Rect(Screen.width / 2, Screen.height / 2 + style.fontSize * 0.5f, buttonWidth, buttonHeight), "Rocket Select"))
        {
            HardRestart();
        }
    }

    void StartScreen()
    {
        if (m_OnStartScreen)
        {
            GUIStyle style = new GUIStyle(m_CustomMenuSkin.box);
            GUIStyle style2 = new GUIStyle(m_CustomMenuSkin.button);
            int buttonWidth = 100;
            int buttonHeight = 25;

            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "", style);

            if (GUI.Button(new Rect(Screen.width / 2 - buttonWidth / 2, Screen.height * 0.7f - (buttonHeight * 2), buttonWidth, buttonHeight), "Easy Rocket", style2))
            {
                m_UserInputStrings[0] = 840.ToString();
                m_UserInputStrings[1] = 0.2.ToString();

                m_UserInputStrings[2] = 2000.ToString();
                m_UserInputStrings[3] = 49000.ToString();
                m_UserInputStrings[4] = 0.3.ToString();

                m_UserInputStrings[5] = 1500.ToString();
                m_UserInputStrings[6] = 820.ToString();
                m_UserInputStrings[7] = 870.ToString();
                m_UserInputStrings[8] = 63.65.ToString();
                m_UserInputStrings[9] = 55.04.ToString();
                m_UserInputStrings[10] = 0.2.ToString();
                m_UserInputStrings[11] = 3.66.ToString();
            }

            if (GUI.Button(new Rect(Screen.width / 2 - buttonWidth / 2, Screen.height * 0.7f - buttonHeight, buttonWidth, buttonHeight), "Start"))
            {
                if (HasValidInputs())
                {
                    m_CrewComponent = new CrewComponent(m_UserInputs[0], m_UserInputs[1]);
                    m_FuelComponent = new FuelComponent(m_UserInputs[2], m_UserInputs[4], m_UserInputs[3]);
                    m_EngineComponent = new EngineComponent(m_UserInputs[5], m_UserInputs[10], m_UserInputs[6], m_UserInputs[7], m_UserInputs[8], m_UserInputs[9]);
                    m_RocketDiameter = m_UserInputs[11];

                    m_OnStartScreen = false;
                    Initialize();
                }
            }

            //Draw User Inputs

            if (!HasValidInputs())
            {
                style.normal.textColor = Color.red;
                GUI.Label(new Rect(Screen.width / 2 - 50, Screen.height * 0.7f - buttonHeight*3, 100, 25), "Invalid Inputs", style);
                style.normal.textColor = Color.white;
            }

            DrawInputTable(Screen.width/4 + 75,Screen.height/4);
        }
    }

    private void DrawInputTable(int xOffset, int yOffset)
    {
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.alignment = TextAnchor.MiddleLeft;

        int columnWidth = 100;
        int rowHeight = 25;

        int currentRowOffset = 0;
        int lastColumn = 0;
        for (int labelIndex = 0; labelIndex < m_MenuEntries.Length; labelIndex++)
        {
            currentRowOffset = m_MenuEntries[labelIndex].column != lastColumn ? 0 : currentRowOffset;

            currentRowOffset++;
            lastColumn = m_MenuEntries[labelIndex].column;

            //Label and Input
            GUI.Label(new Rect(xOffset + (columnWidth * 2 * lastColumn) - 10, yOffset + (rowHeight * currentRowOffset * 2), columnWidth * 2, rowHeight), m_MenuEntries[labelIndex].label, style);
	        m_UserInputStrings[labelIndex] = GUI.TextField(new Rect(xOffset + (columnWidth * 2 * lastColumn), (yOffset + rowHeight) + (rowHeight * currentRowOffset * 2), columnWidth * 0.5f, rowHeight), m_UserInputStrings[labelIndex], MAX_USERINPUT);
        }
    }

    private bool HasValidInputs()
    {
        for (int i = 0; i < m_UserInputs.Length; i++)
        {
            if (float.TryParse(m_UserInputStrings[i], out m_UserInputs[i]))
            {
                if (m_UserInputs[i] == 0)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        return true;
    }

    void RunFrame()
    {
        if (!m_Rocket.HitGround() && !m_OnStartScreen)
        {
            m_Rocket.updateLocationAndVelocity(Time.deltaTime);
        }

        m_FramePosition_x = m_Rocket.getQ(1);
        m_FramePosition_y = m_Rocket.getQ(3);

        m_RocketMarker.transform.position = this.transform.position;

        m_SimulatedTime += Time.deltaTime;
    }
}
