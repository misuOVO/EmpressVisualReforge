using System;
using System.IO;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Terraria;
using Terraria.ID;

namespace EmpressVisualReforge
{
    internal sealed class GlowConfig
    {
        public bool Enabled = true;
        public float Intensity = 1.0f;
        public float Radius = 1.0f;
        public float RainbowSpeed = 1.0f;
    }

    internal static class EmpressGlow
    {
        private const int TexSize = 256;
        private static Texture2D _glow;
        private static GlowConfig _cfg;

        internal static void Load(string dir)
        {
            try
            {
                _cfg = new GlowConfig();
                if (!string.IsNullOrEmpty(dir))
                {
                    string path = Path.Combine(dir, "config.json");
                    if (File.Exists(path))
                    {
                        GlowConfig loaded = JsonConvert.DeserializeObject<GlowConfig>(File.ReadAllText(path));
                        if (loaded != null) _cfg = loaded;
                    }
                    else
                    {
                        File.WriteAllText(path, JsonConvert.SerializeObject(_cfg, Formatting.Indented));
                    }
                }
            }
            catch (Exception ex)
            {
                EmpressMod.LogError("Load config: " + ex.Message);
                _cfg = new GlowConfig();
            }
        }

        internal static void Unload()
        {
            try { _glow?.Dispose(); } catch { }
            _glow = null;
        }

        private static void EnsureTexture()
        {
            if (_glow != null && !_glow.IsDisposed) return;
            GraphicsDevice dev = Main.graphics != null ? Main.graphics.GraphicsDevice : null;
            if (dev == null) return;

            Color[] data = new Color[TexSize * TexSize];
            for (int y = 0; y < TexSize; y++)
            {
                for (int x = 0; x < TexSize; x++)
                {
                    float dx = (x + 0.5f) / TexSize * 2f - 1f;
                    float dy = (y + 0.5f) / TexSize * 2f - 1f;
                    float d = (float)Math.Sqrt(dx * dx + dy * dy);
                    float a = MathHelper.Clamp(1f - d, 0f, 1f);
                    a = a * a;
                    a = a * (0.35f + 0.65f * a);
                    data[y * TexSize + x] = new Color(255, 255, 255, (byte)(a * 255f));
                }
            }
            _glow = new Texture2D(dev, TexSize, TexSize, false, SurfaceFormat.Color);
            _glow.SetData(data);
        }

        internal static void Draw()
        {
            if (_cfg == null || !_cfg.Enabled) return;
            if (Main.gameMenu || Main.spriteBatch == null || Main.graphics == null) return;
            if (Main.mapFullscreen) return;

            EnsureTexture();
            if (_glow == null || _glow.IsDisposed) return;

            float time = (float)Main.GlobalTimeWrappedHourly;
            float zoom = Math.Abs(Main.GameViewMatrix.Zoom.X);
            if (zoom <= 0.01f) zoom = 1f;
            bool flip = Main.LocalPlayer != null && Main.LocalPlayer.gravDir == -1f;
            int H = Main.screenHeight;
            int W = Main.screenWidth;

            // 女皇本体附近不发光: 只让飞出去的外围攻击发光
            Vector2 bossCenter = Vector2.Zero;
            bool bossAlive = false;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n != null && n.active && n.type == NPCID.HallowBoss)
                {
                    bossCenter = n.Center;
                    bossAlive = true;
                    break;
                }
            }

            bool began = false;
            try
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Matrix.Identity);
                began = true;

                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p == null || !p.active || p.type <= 0) continue;

                    Color color;
                    float radius;
                    float intensity;
                    if (!TryGetLight(p.type, time, p.whoAmI, out color, out radius, out intensity)) continue;
                    if (bossAlive && Vector2.DistanceSquared(p.Center, bossCenter) < 180f * 180f) continue;

                    Vector2 pos = Vector2.Transform(p.Center - Main.screenPosition, Main.GameViewMatrix.ZoomMatrix);
                    if (flip) pos.Y = H - pos.Y;

                    float dia = radius * zoom * _cfg.Radius * 2f;
                    if (pos.X < -dia || pos.X > W + dia || pos.Y < -dia || pos.Y > H + dia) continue;

                    float pulse = 1f + 0.06f * (float)Math.Sin(time * 6f + p.whoAmI * 1.3f);
                    float k = MathHelper.Clamp(intensity * _cfg.Intensity * pulse, 0f, 3f);
                    Color tint = new Color(
                        (byte)MathHelper.Clamp(color.R * k, 0f, 255f),
                        (byte)MathHelper.Clamp(color.G * k, 0f, 255f),
                        (byte)MathHelper.Clamp(color.B * k, 0f, 255f),
                        (byte)255);

                    Main.spriteBatch.Draw(_glow, pos, null, tint, 0f,
                        new Vector2(TexSize / 2f, TexSize / 2f),
                        dia / TexSize, SpriteEffects.None, 0f);
                }
            }
            catch (Exception ex)
            {
                EmpressMod.LogError("Draw: " + ex.Message);
            }
            finally
            {
                if (began)
                {
                    try { Main.spriteBatch.End(); } catch { }
                }
            }
        }

        private static bool TryGetLight(int type, float time, int whoAmI,
            out Color color, out float radius, out float intensity)
        {
            float phase = time * 0.25f * (_cfg != null ? _cfg.RainbowSpeed : 1f) + whoAmI * 0.13f;
            switch (type)
            {
                // 剑阵核心 / 太阳舞 是贴着女皇本体的大量射弹, 不发光, 否则整屏会白掉
                case ProjectileID.HallowBossSplitShotCore:
                case ProjectileID.FairyQueenSunDance:
                    color = Color.White; radius = 0f; intensity = 0f; return false;

                case ProjectileID.HallowBossLastingRainbow:
                    color = Tint(Hsv(phase, 0.72f, 1f)); radius = 105f; intensity = 0.55f; return true;
                case ProjectileID.HallowBossRainbowStreak:
                    color = Tint(Hsv(phase * 1.8f, 0.80f, 1f)); radius = 95f; intensity = 0.6f; return true;
                case ProjectileID.HallowBossDeathAurora:
                    color = Tint(Hsv(phase * 0.7f, 0.70f, 1f)); radius = 160f; intensity = 0.6f; return true;
                case ProjectileID.FairyQueenLance:
                    color = Tint(new Color(255, 120, 220)); radius = 105f; intensity = 0.6f; return true;
                case ProjectileID.FairyQueenHymn:
                    color = Tint(new Color(150, 225, 255)); radius = 95f; intensity = 0.55f; return true;
                case ProjectileID.FairyQueenMagicItemShot:
                    color = Tint(new Color(255, 150, 240)); radius = 95f; intensity = 0.55f; return true;
                case ProjectileID.FairyQueenRangedItemShot:
                    color = Tint(Hsv(phase * 1.4f, 0.75f, 1f)); radius = 100f; intensity = 0.55f; return true;
                case ProjectileID.EmpressBlade:
                    color = Tint(new Color(255, 220, 255)); radius = 80f; intensity = 0.45f; return true;
                default:
                    color = Color.White; radius = 0f; intensity = 0f; return false;
            }
        }

        // 白天(狂暴)的光之女皇射弹本身是白/金色, 不应该再套彩虹色
        private static Color Tint(Color nightColor)
        {
            return Main.dayTime ? new Color(255, 240, 205) : nightColor;
        }

        private static Color Hsv(float h, float s, float v)
        {
            h = h - (float)Math.Floor(h);
            float i = h * 6f;
            int ii = (int)i;
            float f = i - ii;
            float p = v * (1f - s);
            float q = v * (1f - f * s);
            float t = v * (1f - (1f - f) * s);
            float r, g, b;
            switch (ii % 6)
            {
                case 0: r = v; g = t; b = p; break;
                case 1: r = q; g = v; b = p; break;
                case 2: r = p; g = v; b = t; break;
                case 3: r = p; g = q; b = v; break;
                case 4: r = t; g = p; b = v; break;
                default: r = v; g = p; b = q; break;
            }
            return new Color((byte)(r * 255f), (byte)(g * 255f), (byte)(b * 255f), (byte)255);
        }
    }

    /// <summary>
    /// 在所有射弹绘制完之后, 把发光射弹的柔光叠加到世界层。
    /// 世界层随后会经过游戏/光影模组的滤镜与 Bloom。
    /// </summary>
    [HarmonyPatch(typeof(Main), "DrawProjectiles")]
    internal static class Patch_DrawProjectiles
    {
        private static void Postfix()
        {
            EmpressGlow.Draw();
        }
    }
}
