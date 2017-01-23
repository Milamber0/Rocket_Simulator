using System.Collections.Generic;
using UnityEngine;

class Rocket : ODE
{
    private Vector3 m_OrbitTarget;
    private float m_Diameter;
    private float m_Altitude;
    private Vector2 m_Acceleration;
    private float m_TotalStructuralMass;
    private bool m_StructuralMassDirty;
    private float m_Throttle;
    private bool m_HitGround = false;

    public double m_Drag;

    private USatm76 m_Atmosphere;
    private List<BaseComponent> m_Components;

    enum ODEType // Defining numbers explicitly to make it easier to quickly lookup values with a glance
    { 
        VelX = 0, 
        PosX = 1, 
        VelY = 2, 
        PosY = 3, 
        VelZ = 4, 
        PosZ = 5, 
        CurrentFuelMass = 6,
        Angle = 7, 
        NumODEType
    };

    public Rocket( float rocketDiameter, double startX, double startY ) 
        : base((int)ODEType.NumODEType)
    {
        m_Diameter      = rocketDiameter;
        m_OrbitTarget   = new Vector3(0, 0, 0);
        m_Components    = new List<BaseComponent>();
        m_Atmosphere    = new USatm76(0);
        m_Acceleration  = new Vector2();


        setS(0.0);                        // Time starts at 0.
        setQ(0.0,   (int)ODEType.VelX);   // Vx starts at 0
        setQ(startX,(int)ODEType.PosX);
        setQ(0.0,   (int)ODEType.VelY);   // Vy starts at 0
        setQ(startY,(int)ODEType.PosY);
        setQ(0.0,   (int)ODEType.VelZ);   // Vz starts at 0
        setQ(0.0,   (int)ODEType.PosZ);   // z starts at 0


        setQ(0/*initialMass*/, (int)ODEType.CurrentFuelMass);
        setQ(0.50 * Mathf.PI/*theta*/, (int)ODEType.Angle); // pitch angle in radians
    }

    public void AddComponent(BaseComponent component)
    {
        m_Components.Add(component);
        
        component.SetParent(this);
        m_StructuralMassDirty = true;
    }

    public float GetAltitude()
    {
        return m_Altitude;
    }

    public bool HitGround()
    {
        return m_HitGround;
    }

    public float GetVelocity()
    {
        return new Vector2((float)getQ((int)ODEType.VelX), (float)getQ((int)ODEType.VelY)).magnitude;
    }

    public Vector3 GetVelocityVector()
    {
        return new Vector3((float)getQ((int)ODEType.VelX), (float)getQ((int)ODEType.VelY), (float)getQ((int)ODEType.VelZ));
    }

    public Vector3 GetPosition()
    {
        return new Vector3((float)getQ((int)ODEType.PosX), (float)getQ((int)ODEType.PosY), (float)getQ((int)ODEType.PosZ));
    }

    public float GetAcceleration()
    {
        return m_Acceleration.magnitude;
    }

    public float GetFuel()
    {
        return (float)getQ((int)ODEType.CurrentFuelMass);
    }

    public double GetDrag()
    {
        return m_Drag;
    }

    public void AddFuel(float fuelMass)
    {
        setQ(getQ((int)ODEType.CurrentFuelMass) + fuelMass, (int)ODEType.CurrentFuelMass);
    }

    public bool SetAngle(double angle)
    {
        if(angle != getQ((int)ODEType.Angle))
        {
            setQ(angle, (int)ODEType.Angle);
            return true;
        }
        return false;
    }

    public void SetThrottle(float throttle)
    {
        m_Throttle = throttle;
    }

    private float GetDragCoefficient()
    {
        // http://wiki.kerbalspaceprogram.com/wiki/Atmosphere#Drag
        // Using structural mass to calculate drag, since fuel mass does not affect drag.
        float totalMass = 0.0f;
        float numerator = 0.0f;
        foreach (BaseComponent component in m_Components)
        {
            float mass = component.GetStructuralMass();
            numerator += mass * component.GetDrag();
            totalMass += mass;
        }

        return numerator / totalMass;
    }

    private float GetTotalMass()
    {
        float totalMass = 0.0f;
        foreach (BaseComponent component in m_Components)
        {
            totalMass += component.GetMass();
        }

        return totalMass;
    }

    private float GetTotalStructuralMass()
    {
        if (m_StructuralMassDirty)
        {
            m_TotalStructuralMass = 0.0f;
            foreach (BaseComponent component in m_Components)
            {
                m_TotalStructuralMass += component.GetStructuralMass();
            }

            m_StructuralMassDirty = false;
        }

        return m_TotalStructuralMass;
    }

    // This method updates the velocity and location
    // of the projectile using a 4th order Runge-Kutta
    // solver to integrate the equations of motion.
    public bool updateLocationAndVelocity(double dt)
    {
        ODESolver.rungeKutta4(this, dt);
        return true;
    }

    public override double[] getRightHandSide(double s, double[] q,
                                     double[] deltaQ, double ds,
                                     double qScale)
    {
        double[] dQ = new double[(int)ODEType.NumODEType];
        double[] newQ = new double[(int)ODEType.NumODEType];
        // Compute the intermediate values of the
        // location and velocity components.
        for (int i = 0; i < (int)ODEType.NumODEType; ++i)
        {
            newQ[i] = q[i] + qScale * deltaQ[i];
        }

        // Assign convenience variables to the intermediate
        // values of the locations and velocities.
        double vx           = newQ[(int)ODEType.VelX];
        double vy           = newQ[(int)ODEType.VelY];
        double vz           = newQ[(int)ODEType.VelZ];
        double x            = newQ[(int)ODEType.PosX];
        double y            = newQ[(int)ODEType.PosY];
        double z            = newQ[(int)ODEType.PosZ];
        double fuelMass     = newQ[(int)ODEType.CurrentFuelMass];
        double theta        = newQ[(int)ODEType.Angle];

        float totalMass = Mathf.Max((float)fuelMass,0.0f) + GetTotalStructuralMass();
        double vtotal = Mathf.Sqrt((float)(vx * vx + vy * vy + vz * vz));

        // Update the values of pressure, density, and
        // temperature based on the current altitude.
        double re = 6356766.0; // Radius of the earth in meters.
        Vector3 orbitTargetVec = m_OrbitTarget - new Vector3((float)x, (float)y, (float)z);
        m_Altitude = (orbitTargetVec.magnitude) - (float)re;

        if (m_Altitude < 0.0)
        {
            m_HitGround = true;
            m_Altitude = 0.0f;
        }

        m_Atmosphere.updateConditions(m_Altitude);
        double pressure = m_Atmosphere.getPressure();
        double density = m_Atmosphere.getDensity();

        // Compute the drag force based on the frontal area
        // of the rocket.
        // Compute the gravitational acceleration
        // as a function of altitude.
        double area = 0.25 * Mathf.PI * m_Diameter * m_Diameter;
        double drag = 0.5 * GetDragCoefficient() * density * vtotal * vtotal * area;
        double g;
        if (m_Altitude > 0.0f)
        {
            m_Drag = drag;
            g = 9.80665 * re * re / Mathf.Pow((float)(re + m_Altitude), 2.0f);
        }
        else
        {
            m_Drag = 0.0; 
            g = 0.0;
        }

        // For this simulation, lift will be assumed to be zero.
        double lift = 0.0;

        // Deal with gravity
        Vector3 gravityVec = orbitTargetVec.normalized;

        // Find current max thrust
        EngineComponent engineComponent = (EngineComponent)m_Components[m_Components.Count - 1];
        double thrust = engineComponent.GetThrust(pressure / 101325.0); //101325.0 atmospheric pressure at "1 atm"
        double massFlowRate = engineComponent.GetFuelConsumption(pressure / 101325.0);

        // Adjusting for throttle and making sure that if we're out of fuel we're not gonna get any thrust.
        // If we've only got partially enough fuel for a full thrust, the thrust force is scaled down.
        double adjustedMassFlowRate = m_Throttle * Mathf.Max(Mathf.Min((float)massFlowRate, (float)fuelMass), 0.0f);
        thrust = thrust * (adjustedMassFlowRate / massFlowRate);

        Vector2 velocityDirection = new Vector2((float)vx, (float)vy).normalized;

        // Compute the force components in the x- and y-directions.
        // The rocket will be assumed to be traveling in the x-y plane.
        double Fx = thrust * Mathf.Cos((float)theta) - lift * Mathf.Sin((float)theta) - m_Drag * (double)velocityDirection.x + totalMass * g * gravityVec.x;
        double Fy = thrust * Mathf.Sin((float)theta) + lift * Mathf.Cos((float)theta) - m_Drag * (double)velocityDirection.y + totalMass * g * gravityVec.y;

        // F = ma  ->  a = f/m 
        m_Acceleration.x = (float)(Fx / totalMass);
        m_Acceleration.y = (float)(Fy / totalMass);

        // Load the right-hand sides of the ODEs.
        dQ[(int)ODEType.VelX]            = ds * m_Acceleration.x;
        dQ[(int)ODEType.PosX]            = ds * vx;
        dQ[(int)ODEType.VelY]            = ds * m_Acceleration.y;
        dQ[(int)ODEType.PosY]            = ds * vy;
        dQ[(int)ODEType.VelZ]            = 0.0; // y-component of acceleration = 0
        dQ[(int)ODEType.PosZ]            = 0.0;
        dQ[(int)ODEType.CurrentFuelMass] = -ds * (adjustedMassFlowRate);
        return dQ;
    }

    public bool SimplifiedPredictNewPosition(Vector3 startPosition, Vector3 startVelocity, float deltaTime, out Vector3 newPosition, out Vector3 newVelocity)
    {
        // Current Altitude
        double re = 6356766.0; // Radius of the earth in meters.
        Vector3 orbitTargetVec = m_OrbitTarget - startPosition;
        float altitude = (orbitTargetVec.magnitude) - (float)re;

        if(altitude <= 0)
        {
            newPosition = new Vector3();
            newVelocity = new Vector3();
            return true;
        }

        // Gravity
        Vector3 gravityVec = orbitTargetVec.normalized;
        double g = 9.80665 * re * re / Mathf.Pow((float)(re + altitude), 2.0f);

        startVelocity.x = (float)(startVelocity.x + (g * gravityVec.x) * deltaTime);
        startVelocity.y = (float)(startVelocity.y + (g * gravityVec.y) * deltaTime);
        newVelocity = startVelocity; 
        newPosition = startPosition + newVelocity * deltaTime;
        return false;
    }
}