import conf from "../util/config.js";
import request from "../util/request.js";
import enums from "../util/enums.js";
import joi from "joi";
import xml2js from 'xml2js'
import responseUtil from "../util/response.js";
import HatTemplate from '../../scripts/Hat.json' with { type: 'json' };
import PackageTemplate from '../../scripts/Package.json' with { type: 'json' };
import BodyPartTemplate from '../../scripts/BodyPart.json' with { type: 'json' };
import MeshTemplate from '../../scripts/Mesh.json' with { type: 'json' };
import MeshPartTemplate from '../../scripts/MeshPart.json' with { type: 'json' };
import HeadTemplate from '../../scripts/Head.json' with { type: 'json' };
import ModelTemplate from '../../scripts/Model.json' with { type: 'json' };
import AnimationSilhouetteTemplate from '../../scripts/AnimationSilhouette.json' with { type: 'json' };
import AnimationTemplate from '../../scripts/AvatarAnimation.json' with { type: 'json' };
const port = enums.CatalogRCC;

export const RequestHatThumbnail = async (req, res) => {
    try {
        const schema = joi.object({
            assetId: joi.number().required().integer(),
            jobExpiration: joi.number().max(60).default(20).integer(),
        })
        const { error } = schema.validate(req.body)
        if (error) {
            return responseUtil(res, 'Invalid form', 400, false, { error: error.message })
        }

        var { assetId, jobExpiration } = req.body
        if (jobExpiration == undefined) { jobExpiration = 20 }
        const assetUrl = `${conf.baseUrl}asset?id=${assetId}`

        const xml = JSON.parse(JSON.stringify(HatTemplate));
        xml.Settings.Arguments[0] = assetUrl;
        xml.Settings.Arguments[4] = conf.baseUrl;

        const response = await request({
            RCC: port,
            XML: xml,
            jobExpiration,
        })
        xml2js.parseString(response.data, (err, jsXmlData) => {
            if (err) {
                return responseUtil(res, 'An internal server error occurred.', 500, false, { data: enums.RenderFailed })
            }
            const xmlData = jsXmlData['SOAP-ENV:Envelope']['SOAP-ENV:Body'][0]['ns1:BatchJobResponse'][0]['ns1:BatchJobResult'][0]['ns1:value'][0];
            return responseUtil(res, 'success', 200, true, { data: xmlData });
        })
    } catch (err) {
        return responseUtil(res, 'An internal server error occurred.', 500, false, { error: err.message })
    }
}


export const RequestAnimationSilhouetteThumbnail = async (req, res) => {
    try {
        const schema = joi.object({
            assetId: joi.number().required().integer(),
            jobExpiration: joi.number().max(60).default(20).integer(),
        })
        const { error } = schema.validate(req.body)
        if (error) {
            return responseUtil(res, 'Invalid form', 400, false, { error: error.message })
        }
        var { assetId, jobExpiration } = req.body
        if (jobExpiration == undefined) { jobExpiration = 20 }
        const assetUrl = `${conf.baseUrl}asset?id=${assetId}`

        const xml = JSON.parse(JSON.stringify(AnimationSilhouetteTemplate));
        xml.Settings.Arguments[0] = assetUrl;
        xml.Settings.Arguments[1] = conf.baseUrl;
        xml.Settings.Arguments[4] = '128/128/128';
        const response = await request({
            RCC: port,
            XML: xml,
            jobExpiration,
        })
        xml2js.parseString(response.data, (err, jsXmlData) => {
            if (err) {
                return responseUtil(res, 'An internal server error occurred.', 500, false, { data: enums.RenderFailed })
            }
            const xmlData = jsXmlData['SOAP-ENV:Envelope']['SOAP-ENV:Body'][0]['ns1:BatchJobResponse'][0]['ns1:BatchJobResult'][0]['ns1:value'][0];
            return responseUtil(res, 'success', 200, true, { data: xmlData });
        })
    } catch (err) {
        return responseUtil(res, 'An internal server error occurred.', 500, false, { error: err.message })
    }
}

export const RequestAnimationThumbnail = async (req, res) => {
    const maxRetries = 5; // Define the maximum number of retries
    let attempt = 0;

    const schema = joi.object({
        characterAppearanceUrl: joi.string().required(),
        animationUrl: joi.string().required(),
        jobExpiration: joi.number().max(60).default(20).integer(),
    });

    const validateRequest = () => {
        const { error } = schema.validate(req.body);
        if (error) {
            throw new Error(`Invalid form: ${error.message}`);
        }
    };

    const processRequest = async () => {
        const { characterAppearanceUrl, animationUrl, jobExpiration = 20 } = req.body;

        const xml = JSON.parse(JSON.stringify(AnimationTemplate));
        xml.Settings.Arguments[0] = characterAppearanceUrl;
        xml.Settings.Arguments[1] = conf.baseUrl;
        xml.Settings.Arguments[5] = animationUrl;

        const response = await request({
            RCC: port,
            XML: xml,
            jobExpiration,
        });

        return new Promise((resolve, reject) => {
            xml2js.parseString(response.data, (err, jsXmlData) => {
                if (err) {
                    return reject(new Error('Failed to parse XML response.'));
                }
                const xmlData = jsXmlData['SOAP-ENV:Envelope']['SOAP-ENV:Body'][0]['ns1:BatchJobResponse'][0]['ns1:BatchJobResult'][0]['ns1:value'][0];
                resolve(xmlData);
            });
        });
    };

    try {
        validateRequest();

        while (attempt < maxRetries) {
            try {
                attempt++;
                const xmlData = await processRequest();
                return responseUtil(res, 'success', 200, true, { data: xmlData });
            } catch (err) {
                if (attempt >= maxRetries) {
                    throw err;
                }
                console.error(`Attempt ${attempt} failed, retrying...`);
            }
        }
    } catch (err) {
        return responseUtil(res, 'An internal server error occurred.', 500, false, { error: err.message });
    }
}

export const RequestModelThumbnail = async (req, res) => {
    try {
        const schema = joi.object({
            assetId: joi.number().required().integer(),
            jobExpiration: joi.number().max(60).default(20).integer(),
        })
        const { error } = schema.validate(req.body)
        if (error) {
            return responseUtil(res, 'Invalid form', 400, false, { error: error.message })
        }

        var { assetId, jobExpiration } = req.body
        if (jobExpiration == undefined) { jobExpiration = 20 }
        const assetUrl = `${conf.baseUrl}asset?id=${assetId}`

        const xml = JSON.parse(JSON.stringify(ModelTemplate));
        xml.Settings.Arguments[0] = assetUrl;
        xml.Settings.Arguments[4] = conf.baseUrl;

        const response = await request({
            RCC: port,
            XML: xml,
            jobExpiration,
        })
        xml2js.parseString(response.data, (err, jsXmlData) => {
            if (err) {
                return responseUtil(res, 'An internal server error occurred.', 500, false, { data: enums.RenderFailed })
            }
            const xmlData = jsXmlData['SOAP-ENV:Envelope']['SOAP-ENV:Body'][0]['ns1:BatchJobResponse'][0]['ns1:BatchJobResult'][0]['ns1:value'][0];
            return responseUtil(res, 'success', 200, true, { data: xmlData });
        })
    } catch (err) {
        return responseUtil(res, 'An internal server error occurred.', 500, false, { error: err.message })
    }
}

export const RequestBodyPartThumbnail = async (req, res) => {
    try {
        const schema = joi.object({
            assetUrl: joi.string().required(),
            jobExpiration: joi.number().max(60).default(20).integer(),
        })
        const { error } = schema.validate(req.body)
        if (error) {
            return responseUtil(res, 'Invalid form', 400, false, { error: error.message })
        }

        var { assetUrl, jobExpiration } = req.body
        if (jobExpiration == undefined) { jobExpiration = 20 }

        const xml = JSON.parse(JSON.stringify(BodyPartTemplate));
        xml.Settings.Arguments[0] = assetUrl;
        xml.Settings.Arguments[1] = conf.baseUrl;
        xml.Settings.Arguments[3] = 1680;
        xml.Settings.Arguments[4] = 1680;
        xml.Settings.Arguments[5] = `${conf.baseUrl}asset/?id=1785197`; // double slash, could cause issue. idk.

        const response = await request({
            RCC: port,
            XML: xml,
            jobExpiration,
        })
        xml2js.parseString(response.data, (err, jsXmlData) => {
            if (err) {
                return responseUtil(res, 'An internal server error occurred.', 500, false, { data: enums.RenderFailed })
            }
            const xmlData = jsXmlData['SOAP-ENV:Envelope']['SOAP-ENV:Body'][0]['ns1:BatchJobResponse'][0]['ns1:BatchJobResult'][0]['ns1:value'][0];
            return responseUtil(res, 'success', 200, true, { data: xmlData });
        })
    } catch (err) {
        return responseUtil(res, 'An internal server error occurred.', 500, false, { error: err.message })
    }
}

export const RequestPackageThumbnail = async (req, res) => {
    try {
        const schema = joi.object({
            assetUrls: joi.string().required(),
            jobExpiration: joi.number().max(60).default(20).integer(),
        })
        const { error } = schema.validate(req.body)
        if (error) {
            return responseUtil(res, 'Invalid form', 400, false, { error: error.message })
        }

        var { assetUrls, jobExpiration } = req.body
        if (jobExpiration == undefined) { jobExpiration = 20 }

        const xml = JSON.parse(JSON.stringify(PackageTemplate));
        xml.Settings.Arguments[0] = assetUrls;
        xml.Settings.Arguments[1] = conf.baseUrl;
        xml.Settings.Arguments[3] = 1680;
        xml.Settings.Arguments[4] = 1680;
        xml.Settings.Arguments[5] = `${conf.baseUrl}asset/?id=1785197`; // double slash, could cause issue. idk.

        const response = await request({
            RCC: port,
            XML: xml,
            jobExpiration,
        })
        xml2js.parseString(response.data, (err, jsXmlData) => {
            if (err) {
                return responseUtil(res, 'An internal server error occurred.', 500, false, { data: enums.RenderFailed })
            }
            const xmlData = jsXmlData['SOAP-ENV:Envelope']['SOAP-ENV:Body'][0]['ns1:BatchJobResponse'][0]['ns1:BatchJobResult'][0]['ns1:value'][0];
            return responseUtil(res, 'success', 200, true, { data: xmlData });
        })
    } catch (err) {
        return responseUtil(res, 'An internal server error occurred.', 500, false, { error: err.message })
    }
}

export const RequestMeshThumbnail = async (req, res) => {
    try {
        const schema = joi.object({
            assetId: joi.number().required().integer(),
            jobExpiration: joi.number().max(60).default(20).integer(),
        })
        const { error } = schema.validate(req.body)
        if (error) {
            return responseUtil(res, 'Invalid form', 400, false, { error: error.message })
        }

        var { assetId, jobExpiration } = req.body
        if (jobExpiration == undefined) { jobExpiration = 20 }
        const assetUrl = `${conf.baseUrl}asset?id=${assetId}`

        const xml = JSON.parse(JSON.stringify(MeshTemplate));
        xml.Settings.Arguments[0] = assetUrl;
        xml.Settings.Arguments[2] = 1260;
        xml.Settings.Arguments[3] = 1260;
        xml.Settings.Arguments[4] = conf.baseUrl;

        const response = await request({
            RCC: port,
            XML: xml,
            jobExpiration,
        })
        xml2js.parseString(response.data, (err, jsXmlData) => {
            if (err) {
                return responseUtil(res, 'An internal server error occurred.', 500, false, { data: enums.RenderFailed })
            }
            const xmlData = jsXmlData['SOAP-ENV:Envelope']['SOAP-ENV:Body'][0]['ns1:BatchJobResponse'][0]['ns1:BatchJobResult'][0]['ns1:value'][0];
            return responseUtil(res, 'success', 200, true, { data: xmlData });
        })
    } catch (err) {
        return responseUtil(res, 'An internal server error occurred.', 500, false, { error: err.message })
    }
}


export const RequestMeshPartThumbnail = async (req, res) => {
    try {
        const schema = joi.object({
            assetId: joi.number().required().integer(),
            jobExpiration: joi.number().max(60).default(20).integer(),
        })
        const { error } = schema.validate(req.body)
        if (error) {
            return responseUtil(res, 'Invalid form', 400, false, { error: error.message })
        }

        var { assetId, jobExpiration } = req.body
        if (jobExpiration == undefined) { jobExpiration = 20 }
        const assetUrl = `${conf.baseUrl}v1/asset?id=${assetId}`

        const xml = JSON.parse(JSON.stringify(MeshPartTemplate));
        xml.Settings.Arguments[0] = assetUrl;
        xml.Settings.Arguments[2] = 1260;
        xml.Settings.Arguments[3] = 1260;
        xml.Settings.Arguments[4] = conf.baseUrl;

        const response = await request({
            RCC: port,
            XML: xml,
            jobExpiration,
        })
        xml2js.parseString(response.data, (err, jsXmlData) => {
            if (err) {
                return responseUtil(res, 'An internal server error occurred.', 500, false, { data: enums.RenderFailed })
            }
            const xmlData = jsXmlData['SOAP-ENV:Envelope']['SOAP-ENV:Body'][0]['ns1:BatchJobResponse'][0]['ns1:BatchJobResult'][0]['ns1:value'][0];
            return responseUtil(res, 'success', 200, true, { data: xmlData });
        })
    } catch (err) {
        return responseUtil(res, 'An internal server error occurred.', 500, false, { error: err.message })
    }
}

export const RequestHeadRender = async (req, res) => {
    try {
        const schema = joi.object({
            assetId: joi.number().required().integer(),
            jobExpiration: joi.number().max(60).default(20).integer(),
        })
        const { error } = schema.validate(req.body)
        if (error) {
            return responseUtil(res, 'Invalid form', 400, false, { error: error.message })
        }

        var { assetId, jobExpiration } = req.body
        if (jobExpiration == undefined) { jobExpiration = 20 }
        const assetUrl = `${conf.baseUrl}asset?id=${assetId}`

        const xml = JSON.parse(JSON.stringify(HeadTemplate));
        xml.Settings.Arguments[0] = assetUrl;
        xml.Settings.Arguments[2] = 1680;
        xml.Settings.Arguments[3] = 1680;
        xml.Settings.Arguments[4] = conf.baseUrl;
        xml.Settings.Arguments[5] = 1785197;

        const response = await request({
            RCC: port,
            XML: xml,
            jobExpiration,
        })
        xml2js.parseString(response.data, (err, jsXmlData) => {
            if (err) {
                return responseUtil(res, 'An internal server error occurred.', 500, false, { data: enums.RenderFailed })
            }
            const xmlData = jsXmlData['SOAP-ENV:Envelope']['SOAP-ENV:Body'][0]['ns1:BatchJobResponse'][0]['ns1:BatchJobResult'][0]['ns1:value'][0];
            return responseUtil(res, 'success', 200, true, { data: xmlData });
        })
    } catch (err) {
        return responseUtil(res, 'An internal server error occurred.', 500, false, { error: err.message })
    }
}