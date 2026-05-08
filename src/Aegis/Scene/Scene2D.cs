using Microsoft.Xna.Framework.Graphics;

namespace Aegis.Scene;

/// <summary>
/// Raiz da hierarquia de cena 2D.
/// Todos os objetos visíveis devem ser filhos (diretos ou indiretos) desta.
/// Equivalente ao h2d.Scene do Heaps.
/// </summary>
public sealed class Scene2D : Object2D
{
    public new void Update(float dt) => base.Update(dt);
    public     void Draw(SpriteBatch sb) => base.Draw(sb, 1f);
}
