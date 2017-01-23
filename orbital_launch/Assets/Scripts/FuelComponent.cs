
class FuelComponent : BaseComponent
{
    float m_FuelMass;

    public FuelComponent(float structureMass, float drag, float fuelMass) 
        : base(structureMass, drag)
    {
        m_FuelMass = fuelMass;
    }

    // In the future, in a perfect world, these components will hold their own fuel. 
    // For this version they're not yet doing that.
    //public bool SpendFuel(float amount)
    //{
    //    m_FuelMass -= amount;
    //    return m_FuelMass > 0.0f;
    //}

    public override float GetMass()
    {
        return m_FuelMass + GetStructuralMass();
    }

    public override void SetParent(Rocket parent)
    {
        base.SetParent(parent);

        // Since we're not gonna be controlling our own fuel in this version, we need to let
        // the rocket know about how much fuel we're contributing.
        m_Parent.AddFuel(m_FuelMass);
    }
}