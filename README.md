# Unity Style Generator

UnityStyleGenerator is a Unity Editor extension that automates the creation of strongly-typed C# classes for styling, replacing hard-coded strings and improving maintainability.

## Features

- Generate C# classes from USS (Unity Style Sheets).
- Eliminate magic strings for style class names.

## Installation

Installing as GIT dependency via Package Manager
1. Open Package Manager (Window -> Package Manager)
2. Click `+` button on the upper left of the window, select "Add mpackage from git URL...'
3. Enter the following URL and click the `Add` button

   ```
   https://github.com/Mercury-Leo/UnityStyleGenerator.git
   ```

## Usage

1. Open Unity and navigate to **Edit > Project Settings > Leo's Tools > Unity Style Generator** or **Tools > .
2. In the Style Generator window:
   - **Input Source**: Select the folder containing your USS files.
   - **Output Path**: Specify where to save the generated `.cs` files.
3. Click **Generate**. The tool will scan your styles and produce corresponding C# classes.
4. All of the USS classes will be available under the prefix 'Style_{ClassName}'

## Example

```csharp

public class StyleExample : MonoBehaviour
{
    void Start()
    {
        // Apply a USS class via generated code
        var buttonStyle = new Button();
        buttonStyle.AddToClassList(Style_Buttons.Primary);
    }
}
```
