package validate

import (
	"fmt"
	"io"
	"log"

	"github.com/robloxapi/rbxfile"
	"github.com/robloxapi/rbxfile/rbxl"
)

func LoadFile(reader io.Reader) (*rbxfile.Root, error) {
	root, warn, err := rbxl.Decoder{}.Decode(reader)
	if err != nil {
		return nil, err
	}
	if warn != nil {
		fmt.Println("[info] read warning:", warn)
	}
	return root, nil
}

func IsItemValid(reader io.Reader) bool {
	file, err := LoadFile(reader)
	if err != nil {
		log.Println("Invalid item file:", err)
		return false
	}
	services := make(map[string]*rbxfile.Instance)
	for _, item := range file.Instances {
		if item.IsService {
			services[item.ClassName] = item
		}
	}
	//log.Println("item data", file, services)
	return len(services) == 0
}

func IsModelValid(reader io.Reader) bool {
	file, err := LoadFile(reader)
	if err != nil {
		log.Println("Invalid model file:", err)
		return false
	}

	for _, inst := range file.Instances {
		if inst.IsService {
			log.Printf("Invalid model: contains service %q\n", inst.ClassName)
			return false
		}
	}

	childSet := make(map[*rbxfile.Instance]struct{})
	for _, inst := range file.Instances {
		for _, child := range inst.Children {
			childSet[child] = struct{}{}
		}
	}

	var rootModels []*rbxfile.Instance
	var rootScripts []*rbxfile.Instance

	for _, inst := range file.Instances {
		if _, isChild := childSet[inst]; isChild {
			continue
		}

		switch inst.ClassName {
		case "Model", "Tool":
			rootModels = append(rootModels, inst)
		case "Script", "LocalScript", "ModuleScript":
			rootScripts = append(rootScripts, inst)
		default:
			log.Printf("Invalid model: unsupported top-level instance class %q\n", inst.ClassName)
			return false
		}
	}

	if len(rootScripts) == 1 && len(rootModels) == 0 {
		return true
	}

	if len(rootModels) == 1 && len(rootScripts) == 0 {
		root := rootModels[0]
		if len(root.Children) == 0 {
			log.Println("Invalid model: root Model has no children.")
			return false
		}

		validClasses := map[string]bool{
			"Part":               true,
			"MeshPart":           true,
			"SpecialMesh":        true,
			"Sky":                true,
			"LuaSourceContainer": true,
			"Script":             true,
			"LocalScript":        true,
			"ModuleScript":       true,
			"Decal":              true,
			"Texture":            true,
		}

		for _, child := range root.Children {
			if !validClasses[child.ClassName] && child.ClassName != "Model" {
				log.Printf("Invalid model: unsupported child class %q in root model.\n", child.ClassName)
				return false
			}
		}

		return true
	}

	log.Printf("Invalid model: expected exactly 1 root Model or 1 root Script, found %d models and %d scripts.\n", len(rootModels), len(rootScripts))
	return false
}

func IsGameValid(reader io.Reader) bool {
	file, err := LoadFile(reader)
	if err != nil {
		log.Println("Invalid place file:", err)
		return false
	}
	services := make(map[string]*rbxfile.Instance)
	for _, item := range file.Instances {
		if item.IsService {
			services[item.ClassName] = item
		}
	}
	//fmt.Println("all services", services)
	if _, exists := services["Lighting"]; !exists {
		return false
	}
	_, workspaceExists := services["Workspace"]
	if !workspaceExists {
		return false
	}

	return true
}
