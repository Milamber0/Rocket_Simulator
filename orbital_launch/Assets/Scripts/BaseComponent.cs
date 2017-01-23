
class BaseComponent
{
    float m_StructureMass;
    float m_Drag;
    protected Rocket m_Parent;

    public BaseComponent(float structureMass, float drag)
    {
        m_StructureMass = structureMass;
        m_Drag          = drag;
    }

    public virtual float GetMass()
    {
        return m_StructureMass;
    }

    public float GetStructuralMass()
    {
        return m_StructureMass;
    }

    public float GetDrag()
    {
        return m_Drag;
    }

    public virtual void SetParent(Rocket parent)
    {
        m_Parent = parent;
    }
}
