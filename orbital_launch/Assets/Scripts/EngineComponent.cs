
class EngineComponent : BaseComponent
{
    private static float G = 9.80665f; // Gravity acceleration

    float m_SpecificImpulseSeaLevel;
    float m_SpecificImpulseVacuum;
    float m_FuelConsumptionSeaLevel;
    float m_FuelConsumptionVacuum;
    float m_ThrustSeaLevel;
    float m_ThrustVacuum;

    public EngineComponent(float structureMass, float drag, float specificImpulseSealevel, float specificImpulseVacuum, float fuelConsumptionSeaLevel, float fuelConsumptionVacuum)
        : base(structureMass, drag)
    {
        m_SpecificImpulseSeaLevel   = specificImpulseSealevel;
        m_SpecificImpulseVacuum     = specificImpulseVacuum;
        m_FuelConsumptionSeaLevel   = fuelConsumptionSeaLevel;
        m_FuelConsumptionVacuum     = fuelConsumptionVacuum;

        m_ThrustSeaLevel    = m_SpecificImpulseSeaLevel * m_FuelConsumptionSeaLevel * G;
        m_ThrustVacuum      = m_SpecificImpulseVacuum * m_FuelConsumptionVacuum * G;
    }

    public double GetThrust(double pressureRatio)
    {
        return m_ThrustVacuum - (m_ThrustVacuum - m_ThrustSeaLevel) * pressureRatio;
    }

    public double GetFuelConsumption(double pressureRatio)
    {
        return m_FuelConsumptionVacuum - (m_FuelConsumptionVacuum - m_FuelConsumptionSeaLevel) * pressureRatio;
    }
}