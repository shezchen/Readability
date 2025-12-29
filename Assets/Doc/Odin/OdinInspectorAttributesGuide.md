# Odin Inspector 常用属性指南

Odin Inspector 是一个强大的 Unity 插件，它提供了大量的属性（Attributes）来帮助开发者定制和增强 Inspector 面板。本指南整理了一些最常用的属性，帮助你快速上手并加速工作流。

## 1. 组织与分组 (Organization & Grouping)

让 Inspector 面板井井有条，避免杂乱无章。

### `[Title("标题")]`
在字段上方添加一个标题，可用于区分不同的设置区域。
```csharp
[Title("基础设置")]
public int Health;

[Title("高级设置", "这里包含一些不常用的配置", TitleAlignments.Centered)]
public float Multiplier;
```

### `[BoxGroup("组名")]`
将多个字段放入一个带边框的盒子中。
```csharp
[BoxGroup("玩家属性")]
public string PlayerName;

[BoxGroup("玩家属性")]
public int Level;
```

### `[TabGroup("组名", "标签页名")]`
创建标签页，非常适合节省垂直空间。
```csharp
[TabGroup("配置", "通用")]
public bool IsEnabled;

[TabGroup("配置", "图形")]
public Color ThemeColor;

[TabGroup("配置", "音频")]
public float Volume;
```

### `[FoldoutGroup("组名")]`
创建一个可折叠的组，默认可以设置为折叠或展开。
```csharp
[FoldoutGroup("调试选项")]
public bool ShowGizmos;

[FoldoutGroup("调试选项")]
public Color GizmoColor;
```

### `[HorizontalGroup]`
将字段水平排列。
```csharp
[HorizontalGroup("Split", 0.5f)]
public float LeftValue;

[HorizontalGroup("Split")]
public float RightValue;
```

---

## 2. 编辑器交互与按钮 (Interaction & Buttons)

在编辑器中直接执行代码或测试功能。

### `[Button]`
在 Inspector 中生成一个按钮，点击时执行对应的方法。非常适合测试逻辑。
```csharp
[Button("打印日志")]
private void LogMessage()
{
    Debug.Log("Hello Odin!");
}

[Button(ButtonSizes.Large)]
private void LargeButton() { ... }
```

### `[OnValueChanged("方法名")]`
当字段值发生变化时，自动调用指定的方法。
```csharp
[OnValueChanged("UpdateMaterial")]
public Color BaseColor;

private void UpdateMaterial()
{
    // 更新材质颜色逻辑
}
```

### `[ShowIf("条件字段或方法")]` / `[HideIf]`
根据条件显示或隐藏字段。
```csharp
public bool HasTexture;

[ShowIf("HasTexture")]
public Texture2D Texture;
```

### `[EnableIf("条件字段或方法")]` / `[DisableIf]`
根据条件启用或禁用字段（变灰）。
```csharp
public bool IsLocked;

[DisableIf("IsLocked")]
public string Password;
```

---

## 3. 数值验证与限制 (Validation & Constraints)

确保输入的数据是合法的。

### `[Range(min, max)]`
Unity 原生也有，但 Odin 的支持更多类型。显示滑动条。
```csharp
[Range(0, 100)]
public int Percentage;
```

### `[MinMaxSlider(min, max)]`
用于设置一个范围（最小值和最大值）。
```csharp
[MinMaxSlider(0, 100)]
public Vector2 MinMaxDamage; // x为min, y为max
```

### `[ValidateInput("验证方法", "错误提示")]`
自定义验证逻辑，如果不通过则在 Inspector 显示错误提示。
```csharp
[ValidateInput("MustBeEven", "数字必须是偶数")]
public int EvenNumber;

private bool MustBeEven(int value)
{
    return value % 2 == 0;
}
```

### `[Required]`
标记字段为必填。如果为空，会显示错误提示。
```csharp
[Required]
public GameObject Prefab;
```

### `[AssetsOnly]` / `[SceneObjectsOnly]`
限制拖入的对象必须是资源文件（Project窗口）或场景对象（Hierarchy窗口）。
```csharp
[AssetsOnly]
public GameObject ProjectPrefab;

[SceneObjectsOnly]
public Transform ScenePoint;
```

---

## 4. 集合与列表 (Collections)

优化 List 和 Array 的显示。

### `[ListDrawerSettings]`
自定义列表的绘制行为，如是否允许添加、删除、拖拽排序等。
```csharp
[ListDrawerSettings(ShowIndexLabels = true, AddCopiesLastElement = true)]
public List<string> Names;
```

### `[TableList]`
将对象列表以表格形式显示，非常适合配置数据。
```csharp
[TableList]
public List<EnemyStats> Enemies;

[Serializable]
public class EnemyStats
{
    public string Name;
    public int Hp;
    public float Speed;
}
```

---

## 5. 其他实用工具 (Utilities)

### `[InfoBox("消息")]`
在字段上方显示提示框。
```csharp
[InfoBox("修改此值可能会导致性能下降", InfoMessageType.Warning)]
public int MaxParticles;
```

### `[LabelText("自定义标签")]`
修改字段在 Inspector 中显示的名称，而不改变变量名。
```csharp
[LabelText("最大生命值")]
public int MaxHP;
```

### `[ValueDropdown("获取列表的方法")]`
显示一个下拉菜单，供选择预定义的值。
```csharp
[ValueDropdown("GetTags")]
public string Tag;

private IEnumerable<string> GetTags()
{
    return new string[] { "Player", "Enemy", "NPC" };
}
```

### `[ShowInInspector]`
强制显示非序列化的字段或属性（Property），通常用于调试查看私有变量或属性的值。
```csharp
[ShowInInspector]
public float CurrentHealth { get; set; }
```
