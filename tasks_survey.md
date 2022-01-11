# Survey
This files holds short descriptions about the tasks that should be done by the users. Users will then describe (rate) the system
for each task that they had to complete using the [NASA-TLX](https://en.wikipedia.org/wiki/NASA-TLX) form.
As a final examination of system usability, the users will complete the 
following questionnarie, [System-Usability-Scale](https://en.wikipedia.org/wiki/System_usability_scale).

## List of tasks for users to complete
For each of the tasks bellow, the user will have to fill out the NASA-TLX form.

1. Load desired data set and open the volume render scene on the HoloLens.
2. Rotate (manipulate) the presented image (volume box) in the desired position.
3. Change the render settings and modes of color compositing.
4. Transfer function drawing basic (should be done for TF 1D (1d canvas), TF 1D (2d canvas) and TF 2D ("ellipse" canvas)
5. Transfer function drawing with global histogram (should be done for TF 1D (1d canvas), TF 1D (2d canvas) and TF 2D ("ellipse" canvas)
6. Transfer function drawing with local histogram (should be done for TF 1D (1d canvas), TF 1D (2d canvas) and TF 2D ("ellipse" canvas)
7. Saving and loading


#### Task 1 - Load desired data set and open the volume render scene on the HoloLens
The user should in main menu select the desired data set for converting, wait for the dataset to be converted and then proceed to the HoloLens connection screen.
After he successfully establishes connection, he should be greeted by the volume visualization screen. 
Not accounting for the time the computer needs for conversion, scene switching and establishing the connection, the task should not take more than 5 minutes.


#### Task 2 - Rotate (manipulate) the presented image (volume cube) in the desired position
The user should manipulate the volume cube such that he/she can observe the desired feature. In case of the test medical image (human skull), the user should
rotate and position the volume cube in a position/orientation that the skull faces the user directly. It should not take more than 5 minutes for the user
to complete this task.

#### Task 3 - Change the render settings and modes of color compositing
The user should change between the different visualization modes (MIP, Surface rendering, "composite), set different min and max values (to filter out), adjust
the sampling size to reduce the noise and change the position of the light and color (for example, to green). Max 8 minutes.

#### Task 4 - Transfer function drawing basic (should be done for TF 1D (1d canvas), TF 1D (2d canvas) and TF 2D ("ellipse" canvas)
User should draw a simple transfer function. Max 5 minutes.

#### Task 5 - Transfer function drawing with global histogram (should be done for TF 1D (1d canvas), TF 1D (2d canvas) and TF 2D ("ellipse" canvas)
User should draw a TF using a global histogram and try coloring only the teeth a specific color (for example blue). Max 10 minutes.

#### Task 6 - Transfer function drawing with local histogram (should be done for TF 1D (1d canvas), TF 1D (2d canvas) and TF 2D ("ellipse" canvas)
User should draw a TF using a local histogram and try coloring only the teeth a specific color (for example blue). Max 10 minutes.

#### Task 7 - Saving and loading
User should draw a TF. Save it, close everything, reset the TF and load the previously stored TF. Max 5 minutes.
