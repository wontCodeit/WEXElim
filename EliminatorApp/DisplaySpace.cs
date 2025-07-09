using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EliminatorApp;

/// <summary>
/// Represents some information needed for drawing a sprite
/// </summary>
/// <param name="Position"> Position in space relative to containing <see cref="RenderTarget2D"/></param>
/// <param name="Rotation"> Rotation in radians relative to containing <see cref="RenderTarget2D"/></param>
public readonly record struct DisplaySpace(Vector2 Position, float Rotation);

