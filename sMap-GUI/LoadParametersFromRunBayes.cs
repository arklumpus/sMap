public string SetParameter(string parameterName, ParameterType parameterType, double[] parameterEstimates, double mean, double variance)
{
    if (parameterType == ParameterType.Rate)
    {
        if (mean > 0.01)
        {
            string mu = Math.Log(mean).ToString(System.Globalization.CultureInfo.InvariantCulture);
            return "LogNormal(" + mu + ", 1)";
        }
        else
        {
            return "Exponential(100)";
        }
    }
    else //ParameterType.Pi or ParameterType.ConditionedProbability
    {
        return "Dirichlet(1)";
    }
}