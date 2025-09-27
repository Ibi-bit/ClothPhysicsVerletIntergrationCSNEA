using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using VectorGraphics;

namespace PhysicsCSAlevlProject;

public class RadialMenu
{
    public List<Tool> _tools;
    public int index = 0;
    public float _radius;
    public float iconSize = 32f;
    public float timeRadialPressed = 0;

    public RadialMenu(List<Tool> tools, float radius, float iconSize = 32f)
    {
        _tools = tools;

        _radius = radius;
        this.iconSize = iconSize;
    }
    public int RadialToolMenuLogic(MouseState mouseState, KeyboardState keyboardState, Vector2 initialMousePosWhenRadialMenuPressed, bool radialMenuPressed, int index, List<Tool> tools)
    {
        if (radialMenuPressed)
        {
            Vector2 mousePos = new Vector2(mouseState.X, mouseState.Y);
            Vector2 dir = mousePos - initialMousePosWhenRadialMenuPressed;
            
            float closestDistance = float.MaxValue;
            int closestIndex = index;
            
            float angleStep = MathHelper.TwoPi / tools.Count;
            for (int i = 0; i < tools.Count; i++)
            {
                float toolAngle = i * angleStep - MathHelper.PiOver2;
                Vector2 toolDirection = new Vector2((float)System.Math.Cos(toolAngle), (float)System.Math.Sin(toolAngle));

                toolDirection.Normalize();
                Vector2 mouseDirection = dir.Length() > 0 ? Vector2.Normalize(dir) : Vector2.Zero;
                float distance = Vector2.DistanceSquared(mouseDirection, toolDirection);
                
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestIndex = i;
                }
            }
            
            if (closestIndex != index)
            {
                index = closestIndex;
            }

            return index;
        }
        return index;
    }
    public void Update()
    {
       
    }
    public void Draw(SpriteBatch spriteBatch, Vector2 position, SpriteFont font = null, PrimitiveBatch primitiveBatch = null)
    {
        float angleStep = MathHelper.TwoPi / _tools.Count;
        for (int i = 0; i < _tools.Count; i++)
        {
            float angle = i * angleStep - MathHelper.PiOver2; 
            Vector2 toolPos = position + new Vector2((float)System.Math.Cos(angle), (float)System.Math.Sin(angle)) * _radius;

            Color toolColor = (i == index) ? Color.Yellow : Color.White;
            
            if (font != null)
            {
                Vector2 textSize = font.MeasureString(_tools[i].Name);
                spriteBatch.DrawString(font, _tools[i].Name, toolPos - textSize / 2, toolColor);
            }
            else if (primitiveBatch != null)
            {
                var circle = new PrimitiveBatch.Circle(toolPos, 10f, toolColor, false);
                circle.Draw(spriteBatch, primitiveBatch);
                
                
                if (i == index)
                {
                    var line = new PrimitiveBatch.Line(position, toolPos, Color.Yellow, 2f);
                    line.Draw(spriteBatch, primitiveBatch);
                }
            }
        }
        
        
        
        
    }
}
