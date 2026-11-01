# 光之女皇 视觉重铸 · Empress Visual Reforge

作者：丷米苏丷

独立的 tPlainModLoader 扩展模组：让泰拉瑞亚 **光之女皇（Empress of Light）** 的外围攻击射弹发光。

## 功能

- 光之女皇的彩虹 / 长枪 / 赞美诗 / 极光等外围射弹带柔光
- 每类射弹独立的颜色、半径、亮度；彩虹类随时间循环变色
- 白天（狂暴）统一为暖白 / 金色，夜晚恢复彩色
- 剑阵核心、太阳舞等贴身材质不发光，避免整屏爆白
- 女皇本体 180 像素范围内的射弹不发光，只有飞出去的外围攻击才亮
- 柔光绘制在世界层，会正常受到游戏光照、阴影与 Bloom 影响
- 不依赖「流光叠影」；两个模组可同时使用

## 构建

需要：

- .NET SDK（项目目标 `net472`）
- XNA Framework 4.0
- Terraria 1.4.5.8 + tPlainModLoader

```powershell
dotnet build -c Release -p:TerrariaDir="D:\steam\steamapps\common\Terraria"
```

默认输出到 `<TerrariaDir>\Mods\EmpressVisualReforge\`。

## 配置

模组目录下自动生成 `config.json`：

```json
{
  "Enabled": true,
  "Intensity": 1.0,
  "Radius": 1.0,
  "RainbowSpeed": 1.0
}
```

## 版权

代码仅供学习与个人使用。Terraria 及相关素材版权归 Re-Logic 所有。
