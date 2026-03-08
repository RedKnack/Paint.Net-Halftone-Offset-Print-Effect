# <img width="1710" height="110" alt="grafik" src="https://github.com/user-attachments/assets/7cf3b0e4-ecc3-48de-9666-87340a41ec76" /> (Paint.NET Plugin)


<img width="1760" height="1760" alt="grafik" src="https://github.com/user-attachments/assets/17ebd2e7-966e-4c45-bfc6-b220583ab867" />

**Halftone Comic/Print** is a **Paint.NET effect plugin** by **RedKnack Interactive**
that simulates real offset print and comic book halftone screens via SDF-based dot rendering.

- Paint.NET: tested with **5.x**
- Language: **C# / .NET 9**
- Location: **Effects → Stylize → Halftone Comic/Print**

---

## Features

- CMYK 4-screen separation with independent per-channel screen angles
- 8 dot shapes: Circle, Diamond, Square, Line, Cross, Ellipse, Euclidean, Ring
- 4 tone curves: Area Proportional, Linear, Gamma, Sine
- SDF-based rendering with configurable soft-edge anti-aliasing
- Black point / white point tone remapping
- 1–4x oversampling
- Alpha-preserving output

---

## Installation

1. Build the project or download the release DLL.
2. Copy `RedKnack.HalftonePlugin.dll` into your Paint.NET effects folder:
   ```
   C:\Program Files\paint.net\Effects
   ```
3. Restart Paint.NET. If necessary.

---

## Parameters

| Parameter | Range | Default |
|---|---|---|
| Cell Size | 2 – 120 px | 20 |
| Dot Shape | Circle … Ring | Circle |
| Tone Curve | Area Proportional … Sine | Area Proportional |
| Color Mode | Grayscale / CMYK / Spot / RGB | CMYK |
| Screen Angle | 0 – 179° | 45° |
| Cyan / Magenta / Yellow / Black Angle | 0 – 179° | 15° / 75° / 0° / 45° |
| Edge Softness | 0 – 10 px | 1.5 |
| Min / Max Dot Size | 0 – 100% | 0% / 95% |
| Invert | bool | false |
| Ring Width | 0.05 – 0.95 | 0.3 |
| Background Color | RGB | 255, 255, 255 |
| Spot Color | RGB | 0, 0, 0 |
| Black Point / White Point | 0–49% / 51–100% | 0% / 100% |
| Oversampling | 1 – 4 | 2 |

---
<img width="347" height="1203" alt="grafik" src="https://github.com/user-attachments/assets/6f1b7aa9-1cd2-4269-992a-82eb64d3cd78" />



## How It Works

> The technical background on printing principles and rendering techniques.

### Halftone Screening
Traditional printing cannot reproduce continuous tones and ink is either on paper or not. Halftone screening solves this by converting tonal values into a grid of variable-size dots. A darker area produces larger dots that cover more paper; a lighter area produces smaller ones. From a reading distance, the eye averages the ink/paper ratio back into a perceived tone, kinda like RGB-Pixels.

This plugin replicates that grid geometry exactly: each output pixel is assigned to the nearest cell center in a rotated coordinate grid, the source image is sampled at that center, and the result drives the dot radius.

### CMYK Separation & Rosette Pattern
Full-color offset printing uses four ink layers: Cyan, Magenta, Yellow, and Black (Key), so CMYK. Each is printed as its own halftone screen at a different angle to prevent the dots from stacking and creating a muddy [moiré](https://en.wikipedia.org/wiki/Moire_(fabric)) . The slight misalignment between the four screens produces the characteristic **rosette pattern** visible under magnification in any printed magazine or comic.

The default angles (C=15°, M=75°, Y=0°, K=45°) match the real-world press standard. Compositing is **subtractive**: cyan ink absorbs red light, magenta absorbs green, yellow absorbs blue, so the same physical absorption model as actual ink on paper.

The K channel uses **[GCR (Grey Component Replacement)](https://en.wikipedia.org/wiki/Grey_component_replacement)**: neutral grey components present in all three CMY channels are replaced by a single black ink hit, reducing ink consumption and improving shadow detail, the same optimization used in professional print workflows.

### SDF Dot Rendering
Each dot shape is defined as a **[Signed Distance Field](https://en.wikipedia.org/wiki/Signed_distance_function)** - a mathematical function that returns the signed distance from any point to the nearest edge of the shape (negative = inside, positive = outside). This means dot boundaries are not hard-aliased pixel edges but smooth, resolution-independent curves. The `soft_edge` parameter controls a smoothstep transition zone around the zero crossing, producing sub-pixel anti-aliasing at no extra sampling cost.

### Euclidean Dot
The Euclidean dot blends between a circle SDF and a square SDF based on the current dot density. At low coverage it reads as isolated circles; at high coverage it fills in as a square, leaving circular white holes, identical to the classic **Euclidean spot** used in high-fidelity print reproduction. This transition is what generates the most accurate rosette geometry in CMYK mode.

### Tone Curves
The relationship between measured tone and physical dot size, other than i thougth, is not linear in print:

- **Area Proportional** → dot area scales linearly with tone value (`r = r_max x √v` or `r equal r_max times squareroot of v`). **Perceptually** correct for print: a 50% tone value covers exactly 50% of the cell area.
- **Gamma (2.2)** → compensates for display gamma, producing visually balanced output when the source image is intended for screen.
- **Sine** → compresses the response at both ends, producing very soft transitions in highlights and shadows.

### Box Filter Cell Sampling
The source image is not sampled at a single point per cell! A box filter averages all pixels within a region proportional to the cell size. This prevents single-pixel noise from creating isolated outlier dots and ensures each dot represents the actual local tone of that print cell, matching how a real world scanner or plate-making system would measure film density (Like on thoose roll-thingies).

---

## License

### Plugin code
MIT License, see `LICENSE`.

### Third-party assets
NONE, NADA, NIENTE

### Thanks to Viktoria for taking the time and explaining in detail how this stuff works ^^
