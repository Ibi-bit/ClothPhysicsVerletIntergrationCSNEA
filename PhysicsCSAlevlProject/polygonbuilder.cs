using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using VectorGraphics;

class PolygonBuilder
{
    int _initialParticle = -1;
    int _finalParticle = -1;
    List<int> _polygonVertices = new List<int>();
    bool _isBuilding = false;

    public PolygonBuilder() { }

    public BuildableMesh Update(
        GameTime gameTime,
        KeyboardState keyboardState,
        KeyboardState previousKeyboardState,
        MouseState mouseState,
        MouseState previousMouseState,
        BuildableMesh mesh
    )
    {
        Vector2 mousePos = new Vector2(mouseState.X, mouseState.Y);

        
        if (
            mouseState.LeftButton == ButtonState.Pressed
            && previousMouseState.LeftButton == ButtonState.Released
        )
        {
            if (!_isBuilding)
            {
                _isBuilding = true;
                _polygonVertices.Clear();


                int newParticleId = mesh.AddParticle(mousePos, 0.1f, false, Color.White);
                _polygonVertices.Add(newParticleId);
                _initialParticle = newParticleId;
                _finalParticle = newParticleId;
            }
            else
            {

                int newParticleId = mesh.AddParticle(mousePos, 0.1f, false, Color.White);


                if (_finalParticle != -1)
                {
                    mesh.AddStickBetween(_finalParticle, newParticleId);
                }

                _polygonVertices.Add(newParticleId);
                _finalParticle = newParticleId;
            }
        }

        
        if (keyboardState.IsKeyDown(Keys.Enter) && !previousKeyboardState.IsKeyDown(Keys.Enter))
        {
            if (_isBuilding && _polygonVertices.Count >= 3)
            {
                
                mesh.AddStickBetween(_finalParticle, _initialParticle);

                
                _isBuilding = false;
                _polygonVertices.Clear();
                _initialParticle = -1;
                _finalParticle = -1;
            }
        }

        
        if (keyboardState.IsKeyDown(Keys.Escape) && !previousKeyboardState.IsKeyDown(Keys.Escape))
        {
            _isBuilding = false;
            _polygonVertices.Clear();
            _initialParticle = -1;
            _finalParticle = -1;
        }

        return mesh;
    }

    public bool IsBuilding => _isBuilding;
    public int VertexCount => _polygonVertices.Count;

    public void Draw(SpriteBatch spriteBatch, PrimitiveBatch primitiveBatch, SpriteFont font)
    {
        if (_isBuilding)
        {
            string instructions =
                $"Building polygon ({VertexCount} vertices). Left click: add vertex, Enter: complete, Esc: cancel";
            spriteBatch.DrawString(font, instructions, new Vector2(10, 140), Color.Yellow);
        }
    }
}
