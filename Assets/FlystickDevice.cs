using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UVRPN.Core;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

// The input system stores a chunk of memory for each device. What that
// memory looks like we can determine ourselves. The easiest way is to just describe
// it as a struct.
//
// Each chunk of memory is tagged with a "format" identifier in the form
// of a "FourCC" (a 32-bit code comprised of four characters). Using
// IInputStateTypeInfo we allow the system to get to the FourCC specific
// to our struct.
public struct FlystickDeviceState : IInputStateTypeInfo
{
    // We use "CUST" here as our custom format code. It can be anything really.
    // Should be sufficiently unique to identify our memory format, though.
    public FourCC format => new FourCC('F', 'L', 'Y', 'S');

    // Next we just define fields that store the state for our input device.
    // The only thing really interesting here is the [InputControl] attributes.
    // These automatically attach InputControls to the various memory bits that
    // we define.
    //
    // To get started, let's say that our device has a bitfield of buttons. Each
    // bit indicates whether a certain button is pressed or not. For the sake of
    // demonstration, let's say our device has 16 possible buttons. So, we define
    // a ushort field that contains the state of each possible button on the
    // device.
    //
    // On top of that, we need to tell the input system about each button. Both
    // what to call it and where to find it. The "name" property tells the input system
    // what to call the control; the "layout" property tells it what type of control
    // to create ("Button" in our case); and the "bit" property tells it which bit
    // in the bitfield corresponds to the button.
    //
    // We also tell the input system about "display names" here. These are names
    // that get displayed in the UI and such.
    [InputControl(name = "firstButton", layout = "Button", bit = 0, displayName = "First Button")]
    [InputControl(name = "secondButton", layout = "Button", bit = 1, displayName = "Second Button")]
    [InputControl(name = "thirdButton", layout = "Button", bit = 2, displayName = "Third Button")]
    [InputControl(name = "fourthButton", layout = "Button", bit = 3, displayName = "Fourth Button")]
    [InputControl(name = "fifthButton", layout = "Button", bit = 4, displayName = "Fifth Button")]
    [InputControl(name = "sixthButton", layout = "Button", bit = 5, displayName = "Sixth Button")]
    public ushort buttons;
}

// Now that we have the state struct all sorted out, we have a way to lay out the memory
// for our device and we have a way to map InputControls to pieces of that memory. What
// we're still missing, however, is a way to represent our device as a whole within the
// input system.
//
// For that, we start with a class derived from InputDevice. We could also base this
// on something like Mouse or Gamepad in case our device is an instance of one of those
// specific types but for this demonstration, let's assume our device is nothing like
// those devices (if we base our devices on those layouts, we have to correctly map the
// controls we inherit from those devices).
//
// Other than deriving from InputDevice, there are two other noteworthy things here.
//
// For one, we want to ensure that the call to InputSystem.RegisterLayout happens as
// part of startup. Doing so ensures that the layout is known to the input system and
// thus appears in the control picker. So we use [InitializeOnLoad] and [RuntimeInitializeOnLoadMethod]
// here to ensure initialization in both the editor and the player.
//
// Also, we use the [InputControlLayout] attribute here. This attribute is optional on
// types that are used as layouts in the input system. In our case, we have to use it
// to tell the input system about the state struct we are using to define the memory
// layout we are using and the controls tied to it.
#if UNITY_EDITOR
[InitializeOnLoad] // Call static class constructor in editor.
#endif
[InputControlLayout(stateType = typeof(FlystickDeviceState))]
public class FlystickDevice : InputDevice, IInputUpdateCallbackReceiver
{
    // [InitializeOnLoad] will ensure this gets called on every domain (re)load
    // in the editor.
#if UNITY_EDITOR
    static FlystickDevice()
    {
        // Trigger our RegisterLayout code in the editor.
        Initialize();
    }

#endif

    // In the player, [RuntimeInitializeOnLoadMethod] will make sure our
    // initialization code gets called during startup.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        // Register our device with the input system. We also register
        // a "device matcher" here. These are used when a device is discovered
        // by the input system. Each device is described by an InputDeviceDescription
        // and an InputDeviceMatcher can be used to match specific properties of such
        // a description. See the documentation of InputDeviceMatcher for more
        // details.
        //
        // NOTE: In case your device is more dynamic in nature and cannot have a single
        //       static layout, there is also the possibility to build layouts on the fly.
        //       Check out the API documentation for InputSystem.onFindLayoutForDevice and
        //       for InputSystem.RegisterLayoutBuilder.
        InputSystem.RegisterLayout<FlystickDevice>(
            matches: new InputDeviceMatcher()
                .WithInterface("Flystick"));
    }

    // While our device is fully functional at this point, we can refine the API
    // for it a little bit. One thing we can do is expose the controls for our
    // device directly. While anyone can look up our controls using strings, exposing
    // the controls as properties makes it simpler to work with the device in script.
    public ButtonControl firstButton { get; private set; }
    public ButtonControl secondButton { get; private set; }
    public ButtonControl thirdButton { get; private set; }
    public ButtonControl fourthButton { get; private set; }
    public ButtonControl fifthButton { get; private set; }
    public ButtonControl sixthButton { get; private set; }

    // FinishSetup is where our device setup is finalized. Here we can look up
    // the controls that have been created.
    protected override void FinishSetup()
    {
        base.FinishSetup();

        firstButton = GetChildControl<ButtonControl>("firstButton");
        secondButton = GetChildControl<ButtonControl>("secondButton");
        thirdButton = GetChildControl<ButtonControl>("thirdButton");
        fourthButton = GetChildControl<ButtonControl>("fourthButton");
        fifthButton = GetChildControl<ButtonControl>("fifthButton");
        sixthButton = GetChildControl<ButtonControl>("sixthButton");
    }

    // We can also expose a '.current' getter equivalent to 'Gamepad.current'.
    // Whenever our device receives input, MakeCurrent() is called. So we can
    // simply update a '.current' getter based on that.
    public static FlystickDevice current { get; private set; }
    public override void MakeCurrent()
    {
        base.MakeCurrent();
        current = this;
    }

    // When one of our custom devices is removed, we want to make sure that if
    // it is the '.current' device, we null out '.current'.
    protected override void OnRemoved()
    {
        base.OnRemoved();
        if (current == this)
            current = null;
    }

    // So, this is all great and nice. But we have one problem. No one is actually
    // creating an instance of our device yet. Which means that while we can bind
    // to controls on the device from actions all we want, at runtime we will never
    // actually receive input from our custom device. For that to happen, we need
    // to make sure that an instance of the device is created at some point.
    //
    // This one's a bit tricky. Because it really depends on how the device is
    // actually discovered in practice. In most real-world scenarios, there will be
    // some external API that notifies us when a device under its domain is added or
    // removed. In response, we would report a device being added (using
    // InputSystem.AddDevice(new InputDeviceDescription { ... }) or removed
    // (using DeviceRemoveEvent).
    //
    // In this demonstration, we don't have an external API to query. And we don't
    // really have another criteria by which to determine when a device of our custom
    // type should be added.
    //
    // So, let's fake it here. First, to create the device, we simply add a menu entry
    // in the editor. Means that in the player, this device will never be functional
    // but this serves as a demonstration only anyway.
    //
    // NOTE: Nothing of the following is necessary if you have a device that is
    //       detected and sent input for by the Unity runtime itself, i.e. that is
    //       picked up from the underlying platform APIs by Unity itself. In this
    //       case, when your device is connected, Unity will automatically report an
    //       InputDeviceDescription and all you have to do is make sure that the
    //       InputDeviceMatcher you supply to RegisterLayout matches that description.
    //
    //       Also, IInputUpdateCallbackReceiver and any other manual queuing of input
    //       is unnecessary in that case as Unity will queue input for the device.

#if UNITY_EDITOR
    [MenuItem("Tools/Flystick Device Sample/Create Device")]
    private static void CreateDevice()
    {
        // This is the code that you would normally run at the point where
        // you discover devices of your custom type.
        InputSystem.AddDevice(new InputDeviceDescription
        {
            interfaceName = "Flystick",
            product = "Sample Product"
        });
    }

    // For completeness sake, let's also add code to remove one instance of our
    // custom device. Note that you can also manually remove the device from
    // the input debugger by right-clicking in and selecting "Remove Device".
    [MenuItem("Tools/Flystick Device Sample/Remove Device")]
    private static void RemoveDevice()
    {
        var customDevice = InputSystem.devices.FirstOrDefault(x => x is FlystickDevice);
        if (customDevice != null)
            InputSystem.RemoveDevice(customDevice);
    }

#endif
    static int _nbButtons = Transform.FindObjectsOfType<VRPN_Button>().Length;
    VRPN_Button[] _buttons = Transform.FindObjectsOfType<VRPN_Button>();
    
    bool[] _pressedButtons = new bool[_nbButtons];
    bool listenersAdded = false; 

    // So the other part we need is to actually feed input for the device. Notice
    // that we already have the IInputUpdateCallbackReceiver interface on our class.
    // What this does is to add an OnUpdate method that will automatically be called
    // by the input system whenever it updates (actually, it will be called *before*
    // it updates, i.e. from the same point that InputSystem.onBeforeUpdate triggers).
    //
    // Here, we can feed input to our devices.
    //
    // NOTE: We don't have to do this here. InputSystem.QueueEvent can be called from
    //       anywhere, including from threads. So if, for example, you have a background
    //       thread polling input from your device, that's where you can also queue
    //       its input events.
    //
    // Again, we don't have actual input to read here. So we just make up some stuff
    // here for the sake of demonstration. We just poll the keyboard
    //
    // NOTE: We poll the keyboard here as part of our OnUpdate. Remember, however,
    //       that we run our OnUpdate from onBeforeUpdate, i.e. from where keyboard
    //       input has not yet been processed. This means that our input will always
    //       be one frame late. Plus, because we are polling the keyboard state here
    //       on a frame-to-frame basis, we may miss inputs on the keyboard.
    //
    // NOTE: One thing we could instead is to actually use OnScreenControls that
    //       represent the controls of our device and then use that to generate
    //       input from actual human interaction.
    public void OnUpdate()
    {
        if (!listenersAdded)
        {
            for (int i = 0; i < _buttons.Length; i++)
            {
                _buttons[i] = Transform.FindObjectsOfType<VRPN_Button>()[i];
                _buttons[i].OnButtonDown.AddListener(SetButtonPressed);
                _buttons[i].OnButtonUp.AddListener(SetButtonUnpressed);
            }

            listenersAdded = true;
        }

        var state = new FlystickDeviceState();

        // Map buttons to 1, 2, and 3.
        for (int i = 0; i < _pressedButtons.Length; i++)
        {
            if (_pressedButtons[i] && i == 0)
                state.buttons |= 1 << 0;
            if (_pressedButtons[i] && i == 1)
                state.buttons |= 1 << 1;
            if (_pressedButtons[i] && i == 2)
                state.buttons |= 1 << 2;
            if (_pressedButtons[i] && i == 3)
                state.buttons |= 1 << 3;    
            if (_pressedButtons[i] && i == 4)
                state.buttons |= 1 << 4;
            if (_pressedButtons[i] && i == 5)
                state.buttons |= 1 << 5;
        }    

        // Finally, queue the event.
        // NOTE: We are replacing the current device state wholesale here. An alternative
        //       would be to use QueueDeltaStateEvent to replace only select memory contents.
        InputSystem.QueueStateEvent(this, state);
    }

    private void SetButtonUnpressed(int i)
    {
        _pressedButtons[i] = false;
    }

    private void SetButtonPressed(int i)
    {
        _pressedButtons[i] = true;
        Debug.Log("Button " + i + " pressed");
    }
}
