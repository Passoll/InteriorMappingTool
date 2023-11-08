# Preprojected Interior Map Atlas Tool

![](Image/4226689723.gif)

Ver: Unity 2020.3.24
## Function
This tool is set up for preprojected atlas resource creation. 
  1. Turning existing cubemap into preprojected map
  2. Turning existing model into preprojected map

**Why we are doing so?** 
Preprojected map allow us to create all the image into one atlas, reducing the images we need. And it can easily realize the effect of day night shifting.

More tech detail can be seen on my blog
https://www.aysebin.top/index.php/archives/394.html

## Usage
- open newtest scene
- For object to Preprojected Map 
  - put the model inside, create an empty object, attach the **Interior mapping Generator** onto it
  - Put all the object into the list ( the day and night mode allow u to put both state on the same image) 
  - Hit **SetCam** and then **RenderAtlas**(if u want to adjust any param on the cam, it is time!)
  - Do not worry about rectangle room, the script will automatically calculate the bounding box and turn it into a square image (we can use the corresponding uv to restretch it back)
  
- For cubemap to Preprojected Map
  - create an empty object, attach the **Interior mapping Generator_Cubemap** onto it
  - Put all the cubemap into the list
  - Hit **Preproject**, the quad will appaer and allow u to adjust the depth of cubemap effect.
  - After ajustment, Hit **merge**
