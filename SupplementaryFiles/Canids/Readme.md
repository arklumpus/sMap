# Files for the reanalysis of Porto et al., 2019
This folder contains the files used for the re-analysis of the Porto et al., 2019 dataset on life-history traits in canids.

This file also contains step-by-step instructions on how to replicate the analysis.

## Phylogenetic analysis:
This analysis was run using BEAST and BEAUTI 1.10.4 (Suchard et al., 2018), with random number seed 1570727503890.
* To review and change BEAST analysis settings, open the file `Canids.beauti` with BEAUTI.
* To run the phylogenetic analysis, open the file `Canids.beast.xml` with BEAST.

## Stochastic mapping:
* To set up a maximum-likelihood analysis using sMap-GUI for each character (e.g. body size):

	1.	Run sMap-GUI and open the sMap Wizard
	2.	Choose the `BodySize.txt` data file
	3.	Choose the `SummaryTree.tre` tree file and confirm
	4.	Edit the rates to set up the model (e.g. for ARD leave them all to "ML", for ER leave one to "ML" and set the others to be equal to that one...) and confirm
	5.	Run the analysis (or save the analysis archive to run it later from the command-line sMap)

	Pre-made analysis archives to run maximum-likelihood analyses are included in the subfolders of this folder, and include `ML` in their name (e.g. `BodySize_ML_ARD.zip`). Be aware that using these files sMap will attempt to use 36 threads, which may result in overload of less powerful computers. In this case, please set up a new analysis using sMap-GUI as described above.

* To set up a Bayesian analysis using sMap-GUI for each character (e.g. body size):
	1.	Run sMap-GUI and open the sMap Wizard
	2.	Choose the `BodySize.txt` data file
	3.	Choose the `PosteriorTreeDistribution.tre` tree file
	4.	Uncheck the "Use consensus" checkbox and choose the `SummaryTree.tre` tree file
	5.	Edit the Pis and set them all to `Dirichlet(1)`
	6.	Edit the rates to set up the model like in the ML analysis. For the rates that were set up as "ML", get the maximum-likelihood estimate (MLE) from the ML analysis and compute its natural logarithm. If the MLE >= 0.01, set the rate as `LogNormal(log(MLE), 1)`, otherwise set it as `Exponential(100)`
	7. Confirm
	8. Click on "Show" to show the advanced settings
	9. Check the checkbox next to "Sample 100 posterior-predictive..." and select "Save them"
	10. Click on "Show" next to "MCMC options" to show the MCMC settings
	11. Check the "Stepping-stone analysis" checkbox
	12. Run the analysis (or save the analysis archive to run it later from the command-line sMap)

	Pre-made analysis archives to run Bayesian analyses with stepping-stone sampling are included in the subfolders of this folder, and include `SS` (for "**S**tepping-**S**tone") in their name (e.g. `BodySize_SS_ARD.zip`).
		
* To run an analysis that you have set up using sMap-GUI with the sMap command-line (or to run it using the provided files):
	1.	Make sure you have the sMap binary in your PATH (or input the full path to it in the command below)
	2.	Open a command-line interface and type:
```sMap -a <BodySize_ML_ARD.zip> -o <output_prefix>```
and press enter
	3.	Wait for the analysis to finish
	
* To blend multiple sMap analyses for one character:
	1.	Gather the log-marginal likelihood estimates for each model
	2.	Compute model posterior probabilities: 
![Model posterior probability formula](http://www.sciweavers.org/tex2img.php?eq=pp_i%20%3D%20%5Cfrac%7B%20%5Cexp%20%5Cleft%20%28%20ML_i%20%5Cright%20%29%20%20%7D%7B%5Csum_%7Bj%3D1%7D%5En%20%5Cexp%20%5Cleft%20%28%20ML_j%20%5Cright%20%29%20%7D&bc=Transparent&fc=Black&im=png&fs=12&ff=modern&edit=0)
Where ![pp_i](http://www.sciweavers.org/tex2img.php?eq=pp_i&bc=Transparent&fc=Black&im=png&fs=12&ff=modern&edit=0) is the posterior probability of model *i* and ![ML_i](http://www.sciweavers.org/tex2img.php?eq=ML_i&bc=Transparent&fc=Black&im=png&fs=12&ff=modern&edit=0) is the estimated log-marginal likelihood for model *i*.
	4.	Run sMap-GUI and open the Blend sMap window
	5.	Load the 6 sMap Bayesian run files and set to each a weight equal to the model posterior probability
	6.	Set the number of blended simulations to 5000
	7.	Click on the "Save blended sMap..." button and save the blended file
		
* To merge the (blended) sMap analyses for the different characters:
	1.	Run sMap-GUI and open the Merge sMap window
	2.	Load the 4 blended sMap files
	3.	Set the number of merged simulations to 5000
	4.	Click on the "Save merged sMap..." button and save the merged file
		
* To perform D-tests:
	1.	Make sure you have the Stat-sMap binary in your PATH (or input the full path to it in the command below)
	2.	Open a command-line interface and type:
```Stat-sMap -s <merged.bin> -t 0 3 dtest_output```
and press enter
	3.	Wait for the D-test to finish (this will take a while...)

	The 0 and the 3 in step 2 refer to the two characters to perform the D-test on. You will want to perform D-tests between "sociality" (character number 3) and each of the other 3 characters, i.e. `0 3`, `1 3` and `2 3`.
	
* To plot the results of the analysis:
	1. Run sMap-GUI and open the Plot sMap window
	2. Load the blended analysis file
	3. Change the plot settings until you are satisfied
	4. When you are ready, press the "Plot preview..." button to show a preview of the plot
	5. Click on the "Save plot..." button to save the plot as a PDF document or PNG image

## References
Porto, L. M. V., Maestri, R., & Duarte, L. D. S. (2019). Evolutionary relationships among life-history traits in Caninae (Mammalia: Carnivora). _Biological Journal of the Linnean Society_. https://doi.org/10.1093/biolinnean/blz069

Suchard, M. A., Lemey, P., Baele, G., Ayres, D. L., Drummond, A. J., & Rambaut, A. (2018). Bayesian phylogenetic and phylodynamic data integration using BEAST 1.10. _Virus Evolution_, _4_(1). https://doi.org/10.1093/ve/vey016