import { useEffect, useRef, useState } from "react";

/**
 * @param {Thumbnail3D} thumbJson
 * @param {RefObject<HTMLElement>} canvasParentRef
 * @param {boolean} isDisplayed
 * @constructor
 */
function Thumbnail3D(thumbJson, canvasParentRef, isDisplayed) {
    const [scene, setScene] = useState(null);
    const [renderer, setRenderer] = useState(null);
    const [camera, setCamera] = useState(null);
    const [orbitControls, setOrbitControls] = useState(null);
    const [isRendering, setIsRendering] = useState(null);
    const canvasRef = useRef(null);
    
    function Cleanup() {
        scene.traverse(object => {
            if (!object.isMesh) return;
            
            if (object.geometry) object.geometry.dispose();
            if (object.material) {
                if (Array.isArray(object.material)) {
                    object.material.forEach((material) => material.dispose());
                } else {
                    object.material.dispose();
                }
            }
            if (object?.material?.map) object.material.map.dispose();
        });
        orbitControls.dispose();
        camera.clear();
        renderer.dispose();
        canvasParentRef.current.removeChild(renderer.domElement);
        scene.clear();
        
        setScene(null);
        setRenderer(null);
        setCamera(null);
        setOrbitControls(null);
        setIsRendering(null);
    }
    
    function Animate() {
        if (!isDisplayed || !thumbJson || !canvasParentRef.current) {
            Cleanup();
        } else {
            requestAnimationFrame(Animate);
        }
        
        orbitControls.update();
        renderer.render(scene, camera);
    }
    
    function Reload3D() {
        let mtlLoader = new THREE.MTLLoader();
        mtlLoader.load(thumbJson.mtl, (materials) => {
            // Set the textures to the textures in the JSON
            if (thumbJson.textures?.length > 0) {
                for (const materialName in materials.materialsInfo) {
                    const info = materials.materialsInfo[materialName];
                    
                    for (const key in info) {
                        if (key.startsWith('map_')) {
                            info[key] = thumbJson.textures[info.d - 1];
                        }
                    }
                }
            }
            materials.preload();
            
            // Now load in the meshes
            let objLoader = new THREE.OBJLoader();
            objLoader.setMaterials(materials);
            objLoader.load(thumbJson.obj, (object) => {
                object.scale.set(1, 1, 1);
                scene.add(object);
                
                // Now render the scene, start the rendering loop
                setIsRendering(false);
                Animate();
            });
        });
    }
    
    useEffect(() => {
        if (isRendering || !isDisplayed || !thumbJson || !canvasParentRef.current) return;
        setIsRendering(true);
        
        // Create the scene
        setScene(new THREE.Scene());
        setCamera(new THREE.PerspectiveCamera(thumbJson.camera.fov));
        setRenderer(new THREE.WebGLRenderer({ alpha: true, antialias: true }));
        setOrbitControls(new THREE.OrbitControls(camera, renderer.domElement, thumbJson));
        
        // Configure the scene
        renderer.setClearColor(0x000000, 0);
        renderer.setSize(352, 352);
        canvasParentRef.current.appendChild(renderer.domElement);
        canvasRef.current = renderer.domElement;
        
        // Lighting
        scene.add(new THREE.AmbientLight(0x808080, 1));
        
        const directionalLight = new THREE.DirectionalLight(0xffffff, 1.2);
        directionalLight.position.set(thumbJson.aabb.max.x, thumbJson.aabb.max.y + 5, thumbJson.aabb.max.z + 5);
        directionalLight.castShadow = false;
        scene.add(directionalLight);
        
        // Now load the asset, including textures
        Reload3D();
        
        return () => {
            Cleanup();
        };
    }, []);
    
    return {
        Cleanup,
        Animate,
        Reload3D,
        
        isRendering,
        canvasRef,
    }
}

export default Thumbnail3D;
