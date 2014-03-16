﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace RT
{
    /// <summary>
    /// GentleBoost algorithm.
    /// See:
    /// <see cref="http://en.wikipedia.org/wiki/AdaBoost"/>,
    /// <see cref="https://github.com/akre54/Comp-Vision/blob/master/assignment4/hw4_qu1/gentleBoost/gentleBoost.m~"/>,
    /// <see cref="http://superluckycat.com/blog/2012/04/03/gentle-boost/"/>
    /// for details.
    /// </summary>
    /// <typeparam name="TWeakLearner">Weak learner (regressor).</typeparam>
    public class GentleBoost<TWeakLearner>
    {
        List<TWeakLearner> learners;

        /// <summary>
        /// Creates a new instance from already trained learners.
        /// <para>Can server during deserialization.</para>
        /// </summary>
        /// <param name="learners">Trained learners.</param>
        public GentleBoost(List<TWeakLearner> learners)
        {
            this.learners = learners;
        }

        /// <summary>
        /// Creates new instance of the GentleBoost algorithm.
        /// </summary>
        public GentleBoost()
            :this(new List<TWeakLearner>())
        {}

        /// <summary>
        /// Gets regression / classification  output.
        /// </summary>
        /// <param name="learnerClassifyFunc">
        /// Learner classification function.
        /// Parameters: learner. 
        /// Returns: learner output.</param>
        /// <returns>Total output for provided sample.</returns>
        public float GetOutput(Func<TWeakLearner, float> learnerClassifyFunc) 
        {
            float output = 0;
            foreach (var learner in learners)
            {
                output += learnerClassifyFunc(learner);
            }

            return output;
        }

        /// <summary>
        /// Trains GentleBoost by adding weak learners <see cref="TWeakLearner"/>.
        /// </summary>
        /// <param name="trueValues">Requested outputs (e.g. for classification the valid values are +1 and -1).</param>
        /// <param name="learnerCreateAndTrainFunc">
        /// Creates and trains learner. 
        /// Parameters: weights of samples. 
        /// Returns: trained learner.
        /// </param>
        /// <param name="learnerClassifyFunc">
        /// Classifies samples using the last trained learner. 
        /// Parameters: learner, sample index. 
        /// Returns: learner output.
        /// </param>
        /// <param name="terminationFunc">
        /// Termination function. 
        /// Parameters: learners, learnerOutputs. 
        /// Returns: true if the training should be stopped, false otherwise.
        /// </param>
        public void Train(float[] trueValues,
                          Func<float[], TWeakLearner> learnerCreateAndTrainFunc, 
                          Func<TWeakLearner, int, float> learnerClassifyFunc,
                          Func<IList<TWeakLearner>, float[], bool> terminationFunc) 
        {
            int nSamples   = trueValues.Length;
            int nPositves  = trueValues.Where(x=> x > 0).Count();
            int nNegatives = nSamples - nPositves;

            float[] outputs = new float[nSamples];
            float[] sampleWeights = new float[nSamples];

            while (terminationFunc(learners, outputs) == false)
            { 
                //compute weights
                float wSum = 0f;

                for (int i = 0; i < nSamples; i++)
                {
                    if (trueValues[i] > 0)
                        sampleWeights[i] = (float)Math.Exp(-1.0 * outputs[i]) / nPositves;
                    else
                        sampleWeights[i] = (float)Math.Exp(+1.0 * outputs[i]) / nNegatives;

                    wSum += sampleWeights[i];
                }

                //normalize weights
                sampleWeights = sampleWeights.Select(x => x / wSum).ToArray();

                //train weak learner
                var trainedLearner = learnerCreateAndTrainFunc(sampleWeights);
                learners.Add(trainedLearner);

                //update outputs
                for (int i = 0; i < nSamples; i++)
                {
                    var learnerOutput = learnerClassifyFunc(trainedLearner, i);
                    outputs[i] += learnerOutput;
                }
            }
        }

        /// <summary>
        /// Gets the underlying weak learners as read-only collection.
        /// </summary>
        public IReadOnlyList<TWeakLearner> Learners { get { return this.learners; } }
    }
}
