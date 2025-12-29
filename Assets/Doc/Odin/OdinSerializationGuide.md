# Odin 序列化功能指南

Unity 原生的序列化系统（`[SerializeField]`）功能有限，不支持字典、接口、泛型等复杂类型。Odin Serializer 弥补了这些短板，允许你序列化几乎任何 C# 类型。

## 1. 核心概念

要使用 Odin 的序列化功能，你的类通常需要继承自 Odin 提供的特殊基类，而不是标准的 `MonoBehaviour` 或 `ScriptableObject`。

### 常用基类
*   **`SerializedMonoBehaviour`**: 替代 `MonoBehaviour`。
*   **`SerializedScriptableObject`**: 替代 `ScriptableObject`。

### 示例
```csharp
using Sirenix.OdinInspector;
using UnityEngine;
using System.Collections.Generic;

// 继承自 SerializedMonoBehaviour
public class Inventory : SerializedMonoBehaviour
{
    // 现在可以直接序列化字典了！
    public Dictionary<string, int> Items = new Dictionary<string, int>();
}
```

---

## 2. 支持的类型

Odin 序列化支持许多 Unity 原生不支持的类型：

### 字典 (Dictionary)
这是最常用的功能之一。
```csharp
public Dictionary<int, GameObject> IdToPrefab;
public Dictionary<string, List<string>> Categories;
```

### 接口 (Interfaces) 与 多态 (Polymorphism)
你可以序列化接口字段，并在 Inspector 中分配实现了该接口的不同类的实例。
```csharp
public interface IWeapon { void Attack(); }

public class Sword : IWeapon { public int Damage; public void Attack() { ... } }
public class Bow : IWeapon { public float Range; public void Attack() { ... } }

public class Hero : SerializedMonoBehaviour
{
    // 在 Inspector 中，你可以选择添加 Sword 或 Bow
    public IWeapon PrimaryWeapon;
    
    // 也可以是接口列表
    public List<IWeapon> AllWeapons;
}
```

### 泛型 (Generics)
```csharp
public class Wrapper<T>
{
    public T Value;
}

public class GameManager : SerializedMonoBehaviour
{
    public Wrapper<int> IntWrapper;
    public Wrapper<GameObject> ObjectWrapper;
}
```

### 多维数组
```csharp
public int[,] Grid = new int[10, 10];
```

---

## 3. 序列化属性 (Attributes)

### `[OdinSerialize]`
如果你不想让字段是 `public` 的，或者你想序列化一个属性（Property），可以使用此属性。它类似于 Unity 的 `[SerializeField]`，但是用于 Odin 的序列化系统。

```csharp
[OdinSerialize]
private Dictionary<string, string> _privateConfig;

[OdinSerialize]
public int SomeProperty { get; set; }
```

### `[NonSerialized]`
阻止字段被序列化。这适用于 Unity 原生序列化和 Odin 序列化。

---

## 4. 序列化策略与注意事项

### 数据存储格式
默认情况下，Odin 将序列化数据存储在 Unity 的序列化系统中（作为一串字节或引用列表）。这意味着它与 Unity 的预制体（Prefab）和变体（Prefab Variant）系统兼容。

### 性能
Odin 序列化通常非常快，但在处理极大量数据时，应注意性能开销。对于简单的类型（如 `int`, `float`, `Vector3`），Unity 原生序列化通常略快一点，但差异微乎其微。

### 平台支持 (AOT)
在 IL2CPP 平台（如 iOS, Android, WebGL）上，Odin 需要生成 AOT（Ahead-of-Time）代码来支持泛型序列化。
*   通常 Odin 会自动处理。
*   如果遇到序列化问题，请检查 `Tools > Odin Inspector > Preferences > Serialization > AOT Generation`。
*   确保在构建项目前运行 AOT 生成扫描。

---

## 5. 常见问题

**Q: 我可以将现有的 MonoBehaviour 改为 SerializedMonoBehaviour 吗？**
A: 可以。但是，如果你之前使用 Unity 原生序列化保存了数据（例如 `List<string>`），切换基类通常不会丢失数据，因为 Odin 默认也会尝试读取 Unity 的序列化数据。但对于字典等 Unity 不支持的类型，你需要重新配置数据。

**Q: 为什么我的字典在 Inspector 里显示了，但是重启游戏后数据丢了？**
A: 确保你继承的是 `SerializedMonoBehaviour` 而不是 `MonoBehaviour`。如果只是在 `MonoBehaviour` 里加了 `[ShowInInspector]`，字典虽然可见，但不会被保存。

**Q: 可以在普通 C# 类中使用 Odin 序列化吗？**
A: 可以，但该类必须被一个 `SerializedMonoBehaviour` 或 `SerializedScriptableObject` 引用。如果该类本身需要多态支持，通常不需要特殊继承，只要引用它的字段支持 Odin 序列化即可。
