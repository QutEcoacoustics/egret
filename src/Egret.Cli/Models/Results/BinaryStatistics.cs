using MathNet.Numerics.LinearAlgebra;
using System;
using System.Numerics;

namespace Egret.Cli.Models.Results
{
    public enum Contingency
    {
        TruePositive,
        FalsePositive,
        TrueNegative,

        FalseNegative
    }

    public record BinaryStatistics
    {
        // https://en.wikipedia.org/wiki/Evaluation_of_binary_classifiers

        private readonly Matrix<double> backing;
        public BinaryStatistics()
        {
            backing = Matrix<double>.Build.Dense(3, 3);
        }
        private BinaryStatistics(Matrix<double> backing)
        {
            this.backing = backing;
        }
        public static BinaryStatistics Empty { get; } = new BinaryStatistics();
        public static BinaryStatistics OneTruePositive { get; } = new BinaryStatistics()
        {
            ConditionPositives = 1,
            PredictedPositives = 1,
            TruePositives = 1
        };
        public static BinaryStatistics OneTrueNegative { get; } = new BinaryStatistics()
        {
            ConditionNegatives = 1,
            PredictedNegatives = 1,
            TrueNegatives = 1
        };
        public static BinaryStatistics OneFalsePositive { get; } = new BinaryStatistics()
        {
            ConditionNegatives = 1,
            PredictedPositives = 1,
            FalsePositives = 1
        };
        public static BinaryStatistics OneFalseNegative { get; } = new BinaryStatistics()
        {
            ConditionPositives = 1,
            PredictedNegatives = 1,
            FalseNegatives = 1
        };
        public static BinaryStatistics OneErroredPositive { get; } = new BinaryStatistics()
        {
            ConditionPositives = 1,
            Errors = 1,
        };
        public static BinaryStatistics OneErroredNegative { get; } = new BinaryStatistics()
        {
            ConditionNegatives = 1,
            Errors = 1,
        };



        public static BinaryStatistics operator +(BinaryStatistics a, BinaryStatistics b)
        {
            return new BinaryStatistics(a.backing + b.backing);
        }

        public static implicit operator BinaryStatistics(Contingency contingency)
        {
            return contingency switch
            {
                Contingency.TruePositive => OneTruePositive,
                Contingency.FalsePositive => OneFalsePositive,
                Contingency.FalseNegative => OneFalseNegative,
                Contingency.TrueNegative => OneTrueNegative,
                _ => throw new InvalidOperationException(),
            };
        }

        // |   | 0                  | 1                  | 2                  |
        // |---|--------------------|--------------------|--------------------|
        // | 0 | Errors             | Condition Positive | Condition Negative |
        // | 1 | Predicted Positive | True Positive      | False Positive     |
        // | 2 | Predicted Negative | False Negative     | True Negative      |

        public double Errors { get => backing[0, 0]; init => backing[0, 0] = value; }
        public double ConditionPositives { get => backing[0, 1]; init => backing[0, 1] = value; }
        public double PredictedPositives { get => backing[1, 0]; init => backing[1, 0] = value; }
        public double TruePositives { get => backing[1, 1]; init => backing[1, 1] = value; }
        public double FalseNegatives { get => backing[2, 1]; init => backing[2, 1] = value; }
        public double ConditionNegatives { get => backing[0, 2]; init => backing[0, 2] = value; }
        public double PredictedNegatives { get => backing[2, 0]; init => backing[2, 0] = value; }
        public double FalsePositives { get => backing[1, 2]; init => backing[1, 2] = value; }
        public double TrueNegatives { get => backing[2, 2]; init => backing[2, 2] = value; }

        public double TotalConditions => ConditionNegatives + ConditionPositives;
        public double TotalResults => PredictedNegatives + PredictedPositives;

        public double Sensitivity => Ratio(TruePositives, ConditionPositives);
        public double Specificity => Ratio(TrueNegatives, ConditionNegatives);
        public double Precision => Ratio(TruePositives, PredictedPositives);
        public double NegativePredictiveValue => Ratio(TrueNegatives, PredictedNegatives);
        public double FalseNegativeRate => Ratio(FalseNegatives, PredictedNegatives);
        public double FalsePositiveRate => Ratio(FalsePositives, ConditionNegatives);
        public double FalseDiscoveryRate => Ratio(FalsePositives, PredictedPositives);
        public double FalseOmissionRate => Ratio(FalseNegatives, PredictedNegatives);
        public double Prevalance => Ratio(ConditionPositives, TotalConditions);

        public double Accuracy => (TruePositives + TrueNegatives) / TotalConditions;

        // this is mainly useful in ensuring all values get cast to doubles before division;
        private static double Ratio(double denominator, double numerator)
        {
            return denominator / numerator;
        }
    }
}