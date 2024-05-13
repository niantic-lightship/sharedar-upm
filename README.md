## Name
sharedar-upm

## Description
The SharedAR UPM is Unity package needed in order to use the SharedAR networking features from Lightship ARDK in your Unity application. This file can be be brought into your project by using the Unity Package Manager. You can install the SharedAR UPM package via a git URL, or from a Tarball (`.tgz`) file. Detailed steps to install the SharedAR UPM can be found in our [developer documentation for Installing the ARDK Plugin](https://lightship.dev/docs/ardk/setup/#installing-the-ardk-plugin-with-a-url). These steps are also noted below:

### Installing the SharedAR Plugin with a URL
0. A prerequisite to using the SharedAR UPM is having the ARDK UPM installed. For more information on installing the ARDK UPM to your project, please see the [ardk-upm ReadMe file](https://github.com/niantic-lightship/ardk-upm/blob/main/README.md). 
1. In your Unity project open the **Package Manager** by selecting **Window > Package Manager**. 
	- From the plus menu on the Package Manager tab, select **Add package from git URL...**
	- Enter `https://github.com/niantic-lightship/sharedar-upm.git`. 
	- Click **Yes** to activate the new Input System Package for AR Foundation 5.0 (if prompted)

### Installing the SharedAR Plugin from Tarball
0. A prerequisite to using the SharedAR UPM is having the ARDK UPM installed. For more information on installing the ARDK UPM to your project, please see the [ardk-upm ReadMe file](https://github.com/niantic-lightship/ardk-upm/blob/main/README.md). 
1. Download the plugin packages (`.tgz`) from the latest release
	- [sharedar-upm](https://github.com/niantic-lightship/sharedar-upm/releases/latest)
2. In your Unity project open the **Package Manager** by selecting **Window > Package Manager**. 
	- From the plus menu on the Package Manager tab, select **Add package from tarball...**
	- Navigate to where you downloaded the SharedAR UPM, select the `.tgz` file you downloaded, and press **Open**. This will install the package in your project's **Packages** folder as the **SharedAR Plugin** folder. 
	- Click **Yes** to activate the new Input System Package for AR Foundation 5.0 (if prompted). 

## More Information on SharedAR
- [SharedAR Feature Page](https://lightship.dev/docs/ardk/features/shared_ar/)
- [SharedAR VPS Sample](https://lightship.dev/docs/ardk/sample_projects/#shared-ar-vps)
- [SharedAR Image Tracking Colocalization Sample](https://lightship.dev/docs/ardk/sample_projects/#shared-ar-image-tracking-colocalization)
- [SharedAR How-To Guides](https://lightship.dev/docs/ardk/how-to/shared_ar/)

## Support
For any other issues, [contact us](https://lightship.dev/docs/ardk/contact_us/) on Discord or the Lightship forums! Before reaching out, open the Console Log by holding three touches on your device's screen for three seconds, then take a screenshot and post it along with a description of your issue.
