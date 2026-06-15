# AutoCADConstraintSolver

一个从 [NoteCAD](https://github.com/NoteCAD/NoteCAD) 移植到 AutoCAD 的参数化约束求解器插件。

## 功能特点

### 草图实体
- 直线、圆弧、圆、椭圆、椭圆弧
- 样条曲线
- 点、文本
- 函数曲线 (y = f(x))
- 偏移实体

### 几何约束
- **重合约束 (Coincident)** - 点与点重合
- **水平/垂直约束 (Horizontal/Vertical)** - 直线水平或垂直
- **平行/垂直约束 (Parallel/Perpendicular)** - 两直线平行或垂直
- **相切约束 (Tangent)** - 圆弧与圆/直线相切
- **等长/等值约束 (Equal Length/Equal Value)** - 两线段等长或两参数等值
- **固定约束 (Fixation)** - 固定点或实体的位置
- **点在实体上 (Point On)** - 点在直线、圆弧、圆等实体上
- **距离约束 (Distance)** - 点-点、点-直线、点-圆、线-线、线-圆、圆-圆距离
- **长度/直径约束 (Length/Diameter)** - 设定线段长度或圆的直径
- **角度约束 (Angle)** - 两直线间的角度
- **中点约束 (Midpoint)** - 点的中点位置
- **同心约束 (Concentric)** - 圆/弧共享圆心
- **等半径约束 (Equal Radius)** - 两圆/弧半径相等
- **对称约束 (Symmetric)** - 点或线的对称关系
- **圆-圆距离约束 (Circles Distance)** - 两圆之间的最小距离
- **线-圆距离约束 (Line-Circle Distance)** - 直线到圆的距离

### 工程约束
- **等式约束** - 自定义参数间的数学关系

## 技术架构

### 核心模块

```
AutoCADConstraintSolver/
├── src/
│   ├── Solver/              # 约束求解器核心 (Newton迭代法)
│   ├── Geometry/            # 几何数学库
│   ├── Entities/            # 草图实体定义
│   ├── Constraints/         # 几何约束实现
│   ├── Converter/           # AutoCAD数据转换层
│   ├── UI/                  # WPF界面层
│   │   ├── Views/           # XAML视图
│   │   ├── ViewModels/      # MVVM视图模型
│   │   └── Controls/        # 自定义控件 (SkiaCanvas)
│   └── Commands/           # AutoCAD命令
├── tests/                   # 单元测试
├── docs/                    # 文档
│   └── DesignAndUserGuide.md  # 设计思路与用户使用说明
└── scripts/                # 构建脚本
```

### 数据流

```
┌─────────────┐    读取     ┌──────────────┐    转换     ┌─────────────┐
│   AutoCAD   │ ──────────> │  Converter   │ ─────────> │   Sketch    │
│   Drawing   │             │    Layer      │            │   Model     │
└─────────────┘             └──────────────┘            └─────────────┘
                                                              │
                                                              v
┌─────────────┐    反作用   ┌──────────────┐    求解     ┌─────────────┐
│   AutoCAD   │ <────────── │  Converter   │ <───────── │   Solver    │
│   Drawing   │             │    Layer      │            │   Engine    │
└─────────────┘             └──────────────┘            └─────────────┘
```

## 支持的AutoCAD版本

- AutoCAD 2021
- AutoCAD 2022
- AutoCAD 2023
- AutoCAD 2024
- AutoCAD 2025
- AutoCAD 2026

## 开发环境要求

### 必需
- **.NET Framework 4.8** 或 **.NET 8.0**
- **Visual Studio 2022** 或更高版本
- **AutoCAD 2021-2026** (用于测试)

### 可选
- **SkiaSharp** - 高性能2D图形渲染
- **AutoCAD .NET API** - ObjectARX包装库

## 安装说明

### 从源码编译

1. 克隆仓库
```bash
git clone https://github.com/dingqile/AutoCADConstraintSolver.git
cd AutoCADConstraintSolver
```

2. 使用 Visual Studio 打开 `AutoCADConstraintSolver.sln`

3. 选择目标 AutoCAD 版本对应的配置进行编译

4. 将编译输出的 DLL 文件复制到 AutoCAD 插件目录

### 部署到 AutoCAD

1. 编译项目生成 `AutoCADConstraintSolver.dll`
2. 在 AutoCAD 中使用 `NETLOAD` 命令加载 DLL
3. 或将 DLL 路径添加到 AutoCAD 支持文件搜索路径

## 使用方法

### 基本操作流程

1. **启动插件**: 在 AutoCAD 命令行输入 `ConstraintSolver` 或点击工具栏按钮

2. **选择草图**: 从 AutoCAD 图纸中选择要添加约束的实体

3. **添加约束**: 使用约束工具栏添加所需的约束

4. **求解**: 系统自动进行约束求解，实时预览求解结果

5. **确认/撤销**: 确认修改或撤销操作

### 快捷命令

| 命令 | 功能 |
|------|------|
| `CS` | 启动约束求解器 |
| `CSLine` | 添加直线 |
| `CSCircle` | 添加圆 |
| `CSCoincident` | 添加重合约束 |
| `CSHorizontal` | 添加水平约束 |
| `CSVertical` | 添加垂直约束 |
| `CSParallel` | 添加平行约束 |
| `CSPerpendicular` | 添加垂直约束 |
| `CSTangent` | 添加相切约束 |
| `CSDistance` | 添加距离约束 |
| `CSAngle` | 添加角度约束 |

## 项目结构详解

### Solver (约束求解器核心)

基于 Newton-Raphson 迭代法的代数约束求解器：

- `EquationSystem.cs` - 方程组管理和求解
- `Expression.cs` - 数学表达式
- `ExpVector.cs` - 向量表达式
- `ExpBasis.cs` - 表达式基函数
- `GaussianMethod.cs` - 高斯消元法

### Geometry (几何数学库)

核心几何计算：

- `Vec2.cs` / `Vec3.cs` - 2D/3D 向量
- `Line2d.cs` / `Circle2d.cs` - 基本图元
- `Arc2d.cs` - 圆弧
- `BBox2d.cs` - 边界框

### Entities (草图实体)

参数化草图实体：

- `PointEntity.cs` - 点实体
- `LineEntity.cs` - 直线实体
- `CircleEntity.cs` - 圆实体
- `ArcEntity.cs` - 圆弧实体
- `EllipseEntity.cs` - 椭圆实体
- `SplineEntity.cs` - 样条曲线实体

### Constraints (约束实现)

各种几何约束的实现：

- `Constraint.cs` - 约束基类
- `CoincidentConstraint.cs` - 重合约束
- `HorizontalConstraint.cs` - 水平约束
- `ParallelConstraint.cs` - 平行约束
- `DistanceConstraint.cs` - 距离约束
- `AngleConstraint.cs` - 角度约束
- 等等...

### Converter (数据转换层)

AutoCAD 与内部数据模型的双向转换：

- `AutoCADEntityConverter.cs` - AutoCAD 实体转换
- `SketchModelConverter.cs` - 草图模型转换
- `ConstraintConverter.cs` - 约束数据转换

## 约束求解算法

### Newton-Raphson 迭代

约束求解器使用 Newton-Raphson 迭代法求解非线性方程组：

```
J(x) * Δx = -F(x)
x_new = x_old + Δx
```

其中：
- J(x) 是 Jacobian 矩阵（方程对参数的偏导数）
- F(x) 是残差向量（约束方程的值）
- Δx 是参数更新量

### 优化策略

1. **稀疏矩阵优化** - 利用约束的稀疏性
2. **变量替换** - 减少方程数量
3. **阻尼因子** - 防止过冲
4. **收敛检测** - 快速判断收敛

## 贡献

欢迎提交 Issue 和 Pull Request！

## 许可证

本项目采用 MIT 许可证 - 详见 [LICENSE](LICENSE) 文件

## 参考项目

- [NoteCAD](https://github.com/NoteCAD/NoteCAD) - 原始的 Unity 实现
- [AutoCAD .NET API](https://help.autodesk.com/view/ACD/2026/ENU/) - 官方文档
- [SharpDxf](https://github.com/mariuszbielanski/SharpDxf) - DXF 文件处理
