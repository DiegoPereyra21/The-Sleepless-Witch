// Pieza de la mesa que necesita las reglas del crafteo. CraftingTable las conecta al empezar.
public interface ICraftingPart
{
    void Bind(CraftingService service);
}
