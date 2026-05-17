using Spookline.SPC.Common;
using UnityEngine;

namespace Spookline.SPC.UI.Overlay {
  public abstract class OverlayField {

    public string label;
    public string unit;
    public Color? color;
    public float lastSeenTime;

    public abstract void Render(IFieldElement element);


    public class String : OverlayField {

      public string value;

      public bool Update(string val) {
        if (value == val) return false;
        value = val;
        return true;
      }

      public override void Render(IFieldElement element) {
        if (element is OverlayFieldElement.Single single) { single.Update(label, value, unit, color); }
      }

    }

    public class Float : OverlayField {

      public float value;
      public int decimals;
      private string _cachedString;

      public bool Update(float val, int dec) {
        if (Mathf.Approximately(value, val) && decimals == dec) return false;
        value = val;
        decimals = dec;
        _cachedString = val.FormatNonAlloc(dec);
        return true;
      }

      public override void Render(IFieldElement element) {
        if (element is OverlayFieldElement.Single single) { single.Update(label, _cachedString, unit, color); }
      }

    }

    public class Int : OverlayField {

      public int value;
      private string _cachedString;

      public bool Update(int val) {
        if (value == val) return false;
        value = val;
        _cachedString = val.FormatNonAlloc();
        return true;
      }

      public override void Render(IFieldElement element) {
        if (element is OverlayFieldElement.Single single) { single.Update(label, _cachedString, unit, color); }
      }

    }

    public class Vec3 : OverlayField {

      public Vector3 value;
      public int decimals;
      private readonly string[] _cachedStrings = new string[3];

      public bool Update(Vector3 val, int dec) {
        if (value == val && decimals == dec) return false;
        value = val;
        decimals = dec;
        _cachedStrings[0] = val.x.FormatNonAlloc(dec);
        _cachedStrings[1] = val.y.FormatNonAlloc(dec);
        _cachedStrings[2] = val.z.FormatNonAlloc(dec);
        return true;
      }

      public override void Render(IFieldElement element) {
        if (element is OverlayFieldElement.Vec3 vector) { vector.Update(label, _cachedStrings, unit, color); }
      }

    }

  }
}