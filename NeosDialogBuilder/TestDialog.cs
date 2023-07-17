using BaseX;
using FrooxEngine;
using System.Collections.Generic;
using System.Linq;

namespace NeosDialogBuilder
{
    /// <summary>
    /// This class demonstrates how to use the library.
    /// </summary>
    internal class TestDialog : IDialog
    {
        [DialogOption("Output")]
        IField<string> output;

        [DialogOption("A Matrix", secret: true)]
        float4x4 matrix;
        [DialogOption("Some Text")]
        string text;

        [DialogAction("Left")]
        public void OnLeft()
        {
            UniLog.Log("OnLeft");
            if (output != null)
            {
                output.World.RunSynchronously(() => output.Value = "OnLeft");
            }
        }

        [DialogAction("Middle", isValidated: false)]
        public void OnMiddle()
        {
            UniLog.Log("OnMiddle");
            if (output != null)
            {
                output.World.RunSynchronously(() => output.Value = "OnMiddle");
            }
        }

        [DialogAction("Right", onlyValidating: new string[] { "text" })]
        public void OnRight()
        {
            UniLog.Log("OnRight");
            if (output != null)
            {
                output.World.RunSynchronously(() => output.Value = "OnRight");
            }
        }

        public void OnDestroy()
        {
            UniLog.Log("OnDestroy");
            if (output != null)
            {
                output.World.RunSynchronously(() => output.Value = "OnDestroy");
            }
        }

        public IDictionary<string, string> UpdateAndValidate()
        {
            UniLog.Log($"Validate {matrix} {text} {output}");
            var errors = new Dictionary<string, string>();
            if (matrix.Determinant == 0.0)
            {
                errors.Add(nameof(matrix), "Determinant == 0");
            }
            else if (matrix.Determinant == 1.0)
            {
                errors.Add(nameof(matrix), "Determinant == 1");
            }
            if (text == null || !text.Any())
            {
                errors.Add(nameof(text), "Missing text");
            }
            else if (text.ToLowerInvariant() == text && matrix.m00 == 0.0)
            {
                errors.Add("special", "lowercase text and matrix_0_0 == 0");
            }
            if (output != null)
            {
                output.World.RunSynchronously(() => output.Value = $"{matrix} {text}");
            }
            UniLog.Log($"Validated: {errors}");
            return errors;
        }
    }
}
