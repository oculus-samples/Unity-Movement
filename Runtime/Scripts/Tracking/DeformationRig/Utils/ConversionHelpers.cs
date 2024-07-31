// Copyright (c) Meta Platforms, Inc. and affiliates.

namespace DeformationRig.Utils
{
    /// <summary>
    /// A class with some common conversions used across the library. Since most of these are just
    /// multiplication with a constant, this ends up being more readable and tidier than having those
    /// constants scattered across classes.
    /// </summary>
    public static class ConversionHelpers
    {
        /// <summary>
        /// Converts a float representing a percentage (i.e. 100 representing 100%) into its decimal
        /// equivalent (i.e. 100f -> 1f).
        /// </summary>
        public static float PercentToDecimal(float percent) => percent * 0.01f;

        /// <summary>
        /// Converts a float representing a percentage in decimal (i.e. 0.5 representing 50%)
        /// into its percentage equivalent (i.e. 0.5f -> 50f).
        /// </summary>
        public static float DecimalToPercent(float dec) => dec * 100f;
    }
}
