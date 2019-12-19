# Files for the simulated data analysis
This folder contains the files used for the analysis on simulated data about a hypothetical bacterial metabolism.

The dataset consists of a tree file, `tree.tree` and of presence/absence data for three "genes" (`geneA.txt`, `geneB.txt` and `geneC.txt`) and a "metabolism" (`metabolism.txt`). Files combining this data are also provided for convenience (`geneA_geneB_metabolism.txt` and `geneA_geneB_geneC_metabolism.txt`).

This file also contains step-by-step instructions on how to replicate the analysis.

## Analysis 1: metabolism as an independent character
In this analysis the "metabolism" trait will be treated as an single (independent) character.

* To set up a maximum-likelihood analysis using sMap-GUI:
	
	1.	Run sMap-GUI and open the sMap Wizard
	2.	Choose the `metabolism.txt` data file
	3.	Choose the `tree.tre` tree file and confirm
	4.	Edit the rates to set up the model (i.e. for ARD leave them all to "ML", for ER leave one to "ML" and set the other to be equal to the first one) and confirm
	5.	Run the analysis (or save the analysis archive to run it later from the command-line sMap)

	Pre-made analysis archives to run maximum-likelihood analyses are included in the `sMap_analysis1_independent` subfolder of this folder, and include `ML` in their name (e.g. `metabolism_ML_ARD.zip`). Be aware that using these files sMap will attempt to use 36 threads, which may result in overload of less powerful computers. In this case, please set up a new analysis using sMap-GUI as described above.

* To set up a Bayesian analysis using sMap-GUI:
	1.	Run sMap-GUI and open the sMap Wizard
	2.	Choose the `metabolism.txt` data file
	3.	Choose the `tree.tre` tree file
	4.	Edit the Pis and set them both to `Dirichlet(1)`
	5.	Edit the rates to set up the model like in the ML analysis. For the rates that were set up as "ML", get the maximum-likelihood estimate (MLE) from the ML analysis and compute its natural logarithm. If the MLE >= 0.01, set the rate as `LogNormal(log(MLE), 1)`, otherwise set it as `Exponential(100)`
	6. Confirm
	7. Click on "Show" to show the advanced settings
	8. Click on "Show" next to "MCMC options" to show the MCMC settings
	9. Check the "Stepping-stone analysis" checkbox
	10. Run the analysis (or save the analysis archive to run it later from the command-line sMap)

	Pre-made analysis archives to run Bayesian analyses with stepping-stone sampling are included in the `sMap_analysis1_independent` subfolder of this folder, and include `SS` (for "**S**tepping-**S**tone") in their name (e.g. `metabolism_SS_ARD.zip`).

* To run an analysis that you have set up using sMap-GUI with the sMap command-line (or to run it using the provided files):
	1.	Make sure you have the sMap binary in your PATH (or input the full path to it in the command below)
	2.	Open a command-line interface and type:
```sMap -a <metabolism_ML_ARD.zip> -o <output_prefix>```
and press enter
	3.	Wait for the analysis to finish
	
* To blend the sMap analyses for the ARD and ER models:
	1.	Gather the log-marginal likelihood estimates for each model
	2.	Compute model posterior probabilities: 
![Model posterior probability formula](http://www.sciweavers.org/tex2img.php?eq=pp_i%20%3D%20%5Cfrac%7B%20%5Cexp%20%5Cleft%20%28%20ML_i%20%5Cright%20%29%20%20%7D%7B%5Csum_%7Bj%3D1%7D%5En%20%5Cexp%20%5Cleft%20%28%20ML_j%20%5Cright%20%29%20%7D&bc=Transparent&fc=Black&im=png&fs=12&ff=modern&edit=0)
Where ![pp_i](http://www.sciweavers.org/tex2img.php?eq=pp_i&bc=Transparent&fc=Black&im=png&fs=12&ff=modern&edit=0) is the posterior probability of model *i* and ![ML_i](http://www.sciweavers.org/tex2img.php?eq=ML_i&bc=Transparent&fc=Black&im=png&fs=12&ff=modern&edit=0) is the estimated log-marginal likelihood for model *i*.
	3.	Run sMap-GUI and open the Blend sMap window
	4.	Load the 2 sMap Bayesian run files and set to each a weight equal to the model posterior probability
	5.	Set the number of blended simulations to 2000
	6.	Click on the "Save blended sMap..." button and save the blended file

* To plot the results of the analysis:
	1. Run sMap-GUI and open the Plot sMap window
	2. Load the blended analysis file
	3. Change the plot settings until you are satisfied
	4. When you are ready, press the "Plot preview..." button to show a preview of the plot
	5. Click on the "Save plot..." button to save the plot as a PDF document or PNG image

## Analysis 2: metabolism conditioned on gene A and gene B
In this analysis, the "metabolism" character will be treated as a character that is conditioned on both "gene A" and "gene B".

* To set up maximum likelihood analyses for "gene A" and "gene B" using sMap-GUI, please follow the instructions in the previous analysis, using the `geneA.txt` and `geneB.txt` data files.

	Pre-made analysis archives to run maximum-likelihood analyses are also included in the `sMap_analysis2_conditioned_AB` subfolder of this folder, and include "ML" in their name (e.g. `geneA_ML_ARD.zip`).

* To set up a Bayesian analysis using sMap-GUI:
	1.	Run sMap-GUI and open the sMap Wizard
	2.	Choose the `geneA_geneB_metabolism.txt` data file
	3.	Choose the `tree.tre` tree file
	4.	Edit the dependency model. Drag and drop character 2 (metabolism) onto character 0 (gene A) and choose "Condition 2 on 0". Drag and drop character 2 onto character 1 (gene B) and choose "Condition 2 on 1". Finally, confirm.
	5.	Edit the Pis and set them all to `Dirichlet(1)`
	6.	Edit the conditioned probabilities and set them all to `Dirichlet(1)`
	7.	Edit the rates to set up the model like in the ML analysis. For the rates that were set up as "ML", get the maximum-likelihood estimate (MLE) from the ML analysis and compute its natural logarithm. If the MLE >= 0.01, set the rate as `LogNormal(log(MLE), 1)`, otherwise set it as `Exponential(100)`
	8. Confirm
	9. Click on "Show" to show the advanced settings
	10. Click on "Show" next to "MCMC options" to show the MCMC settings
	11. Check the "Stepping-stone analysis" checkbox
	12. Run the analysis (or save the analysis archive to run it later from the command-line sMap)

	Repeat these steps for each combination of models (i.e. ARD for gene A and ARD for gene B, ARD for gene A and ER for gene B, ER for gene A and ARD for gene B, ER for gene A and ER for gene B).
	
	Pre-made analysis archives to run Bayesian analyses with stepping-stone sampling are included in the `sMap_analysis2_conditioned_AB` subfolder of this folder, and include `SS` (for "**S**tepping-**S**tone") in their name (e.g. `condAB_AA_SS.zip`). The two letters between `condAB` and `SS.zip` denote the model being used (e.g. `AA` means ARD for both genes, while `EA` means ER for gene A and ARD for gene B).

* Follow the instructions in the previous analysis to blend the sMap analyses for the four combinations of models and to plot the results of the analysis.

## Analysis 3: metabolism conditioned on genes A, B and C
In this analysis, the "metabolism" character will be treated as a character that is conditioned on "gene A", "gene B" and "gene C".

* To set up maximum likelihood analyses for "gene C" using sMap-GUI, please follow the instructions in the first analysis, using the `geneC.txt` data file.

	Pre-made analysis archives to run maximum-likelihood analyses are also included in the `sMap_analysis3_conditioned_ABC` subfolder of this folder, and include "ML" in their name (e.g. `geneC_ML_ARD.zip`).

* To set up Bayesian analyses using sMap-GUI, please follow the instructions in the previous analysis, using the `geneA_geneB_geneC_metabolism.txt` data file. Set the metabolism character (character number 3) to be conditioned on the other three characters (0 - gene A, 1 - gene B and 2 - gene C).

	Repeat this step for each of the 8 combinations of models (e.g. ARD for gene A, ARD for gene B and ARD for gene C; ARD for gene A, ARD for gene B and ER for gene C...).
	
	Pre-made analysis archives to run Bayesian analyses with stepping-stone sampling are included in the `sMap_analysis3_conditioned_ABC` subfolder of this folder, and include `SS` (for "**S**tepping-**S**tone") in their name (e.g. `cond_AAA_SS.zip`). The three letters between `cond` and `SS.zip` denote the model being used (e.g. `AAA` means ARD for all three genes, while `AEA` means ARD for genes A and C and ER for gene B).

* Follow the instructions in the first analysis to blend the sMap analyses for the four combinations of models and to plot the results of the analysis.


## Analysis 4: metabolism conditioned on genes A, B and C with known conditioned probabilities

In this analysis, the "metabolism" character will be treated as a character that is conditioned on "gene A", "gene B" and "gene C". Furthermore, we will assume that we know the correct value of the conditioned probabilities (because we assume that we know how the three genes interact to give rise to the metabolism).

* To set up Bayesian analyses using sMap-GUI, please follow the instructions in the previous analysis. Fix the conditioned probabilities as follows:

	|Probability|Value|Probability|Value|
	|--|--|--|--|
	|P(0 \| A,A,A)|1|P(0 \| P,A,A)|1|
	|P(1 \| A,A,A)|0|P(1 \| P,A,A)|0|
	|P(0 \| A,A,P)|1|P(0 \| P,A,P)|0|
	|P(1 \| A,A,P)|0|P(1 \| P,A,P)|1|
	|P(0 \| A,P,A)|1|P(0 \| P,P,A)|0|
	|P(1 \| A,P,A)|0|P(1 \| P,P,A)|1|
	|P(0 \| A,P,P)|1|P(0 \| P,P,P)|0|
	|P(1 \| A,P,P)|0|P(1 \| P,P,P)|1|

* Repeat this step for each of the 8 combinations of models for the conditioning characters.

* Pre-made analysis archives to run Bayesian analyses with stepping-stone sampling are included in the `sMap_analysis4_conditioned_ABC_known` subfolder of this folder, and include `SS` (for "**S**tepping-**S**tone") in their name (e.g. `known_AAA_SS.zip`). The three letters between `known` and `SS.zip` denote the model being used (e.g. `AAA` means ARD for all three genes, while `AEA` means ARD for genes A and C and ER for gene B).

* Follow the instructions in the first analysis to blend the sMap analyses for the four combinations of models and to plot the results of the analysis.
