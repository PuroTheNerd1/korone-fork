import conf from "../util/config.js";
import request from "../util/request.js";
import enums from "../util/enums.js";
import joi from "joi";
import xml2js from 'xml2js';
import responseUtil from "../util/response.js";
import HatTemplate from '../../scripts/Hat.json' with { type: 'json' };
import PackageTemplate from '../../scripts/Package.json' with { type: 'json' };
import BodyPartTemplate from '../../scripts/BodyPart.json' with { type: 'json' };
import MeshTemplate from '../../scripts/Mesh.json' with { type: 'json' };
import HeadTemplate from '../../scripts/Head.json' with { type: 'json' };
import ModelTemplate from '../../scripts/Model.json' with { type: 'json' };
import AnimationSilhouetteTemplate from '../../scripts/AnimationSilhouette.json' with { type: 'json' };
import AnimationTemplate from '../../scripts/AvatarAnimation.json' with { type: 'json' };

const port = enums.CatalogRCC;
const requestQueue = [];
let isProcessingQueue = false;

const processQueue = async () => {
    if (isProcessingQueue || requestQueue.length === 0) return;
    isProcessingQueue = true;

    const { req, res, xmlTemplate, schema, prepareArguments } = requestQueue.shift();
    try {
        const { error } = schema.validate(req.body);
        if (error) {
            responseUtil(res, 'Invalid form', 400, false, { error: error.message });
        } else {
            const xml = JSON.parse(JSON.stringify(xmlTemplate));
            prepareArguments(xml, req.body);

            const response = await request({
                RCC: port,
                XML: xml,
                jobExpiration: req.body.jobExpiration || 20,
            });

            xml2js.parseString(response.data, (err, jsXmlData) => {
                if (err) {
                    responseUtil(res, 'An internal server error occurred.', 500, false, { data: enums.RenderFailed });
                } else {
                    const xmlData = jsXmlData['SOAP-ENV:Envelope']['SOAP-ENV:Body'][0]['ns1:BatchJobResponse'][0]['ns1:BatchJobResult'][0]['ns1:value'][0];
                    responseUtil(res, 'success', 200, true, { data: xmlData });
                }
            });
        }
    } catch (err) {
        responseUtil(res, 'An internal server error occurred.', 500, false, { error: err.message });
    } finally {
        isProcessingQueue = false;
        processQueue();
    }
};

const enqueueRequest = (req, res, xmlTemplate, schema, prepareArguments) => {
    requestQueue.push({ req, res, xmlTemplate, schema, prepareArguments });
    processQueue();
};

export const RequestHatThumbnail = (req, res) => {
    enqueueRequest(req, res, HatTemplate, joi.object({
        assetId: joi.number().required().integer(),
        jobExpiration: joi.number().max(60).default(20).integer(),
    }), (xml, body) => {
        xml.Settings.Arguments[0] = `${conf.baseUrl}asset?id=${body.assetId}`;
        xml.Settings.Arguments[4] = conf.baseUrl;
    });
};

export const RequestAnimationSilhouetteThumbnail = (req, res) => {
    enqueueRequest(req, res, AnimationSilhouetteTemplate, joi.object({
        assetId: joi.number().required().integer(),
        jobExpiration: joi.number().max(60).default(20).integer(),
    }), (xml, body) => {
        xml.Settings.Arguments[0] = `${conf.baseUrl}asset?id=${body.assetId}`;
        xml.Settings.Arguments[1] = conf.baseUrl;
        xml.Settings.Arguments[4] = '128/128/128';
    });
};

export const RequestAnimationThumbnail = (req, res) => {
    enqueueRequest(req, res, AnimationTemplate, joi.object({
        characterAppearanceUrl: joi.string().required(),
        animationUrl: joi.string().required(),
        jobExpiration: joi.number().max(60).default(20).integer(),
    }), (xml, body) => {
        xml.Settings.Arguments[0] = body.characterAppearanceUrl;
        xml.Settings.Arguments[1] = conf.baseUrl;
        xml.Settings.Arguments[5] = body.animationUrl;
    });
};

export const RequestModelThumbnail = (req, res) => {
    enqueueRequest(req, res, ModelTemplate, joi.object({
        assetId: joi.number().required().integer(),
        jobExpiration: joi.number().max(60).default(20).integer(),
    }), (xml, body) => {
        xml.Settings.Arguments[0] = `${conf.baseUrl}asset?id=${body.assetId}`;
        xml.Settings.Arguments[4] = conf.baseUrl;
    });
};

export const RequestBodyPartThumbnail = (req, res) => {
    enqueueRequest(req, res, BodyPartTemplate, joi.object({
        assetUrl: joi.string().required(),
        jobExpiration: joi.number().max(60).default(20).integer(),
    }), (xml, body) => {
        xml.Settings.Arguments[0] = body.assetUrl;
        xml.Settings.Arguments[1] = conf.baseUrl;
        xml.Settings.Arguments[3] = 1680;
        xml.Settings.Arguments[4] = 1680;
        xml.Settings.Arguments[5] = `${conf.baseUrl}asset/?id=1785197`;
    });
};

export const RequestPackageThumbnail = (req, res) => {
    enqueueRequest(req, res, PackageTemplate, joi.object({
        assetUrls: joi.string().required(),
        jobExpiration: joi.number().max(60).default(20).integer(),
    }), (xml, body) => {
        xml.Settings.Arguments[0] = body.assetUrls;
        xml.Settings.Arguments[1] = conf.baseUrl;
        xml.Settings.Arguments[3] = 1680;
        xml.Settings.Arguments[4] = 1680;
        xml.Settings.Arguments[5] = `${conf.baseUrl}asset/?id=1785197`;
    });
};

export const RequestMeshThumbnail = (req, res) => {
    enqueueRequest(req, res, MeshTemplate, joi.object({
        assetId: joi.number().required().integer(),
        jobExpiration: joi.number().max(60).default(20).integer(),
    }), (xml, body) => {
        xml.Settings.Arguments[0] = `${conf.baseUrl}asset?id=${body.assetId}`;
        xml.Settings.Arguments[2] = 1260;
        xml.Settings.Arguments[3] = 1260;
        xml.Settings.Arguments[4] = conf.baseUrl;
    });
};

export const RequestHeadRender = (req, res) => {
    enqueueRequest(req, res, HeadTemplate, joi.object({
        assetId: joi.number().required().integer(),
        jobExpiration: joi.number().max(60).default(20).integer(),
    }), (xml, body) => {
        xml.Settings.Arguments[0] = `${conf.baseUrl}asset?id=${body.assetId}`;
        xml.Settings.Arguments[2] = 1680;
        xml.Settings.Arguments[3] = 1680;
        xml.Settings.Arguments[4] = conf.baseUrl;
        xml.Settings.Arguments[5] = 1785197;
    });
};