import joi from "joi";
import xml2js from 'xml2js';
import fs from 'fs';
import sharp from 'sharp';
import conf from "../util/config.js";
import request from "../util/request.js";
import enums from "../util/enums.js";
import responseUtil from "../util/response.js";
import DecalTemplate from '../../scripts/Decal.json' with { type: 'json' };
import ImageTemplate from '../../scripts/Image.json' with { type: 'json' };
import ClothingTemplate from '../../scripts/Clothing.json' with { type: 'json' };
const port = enums.ImageRCC;

const renderQueue = [];
let isRendering = false;
const enqueueRender = (renderTask) => {
    return new Promise((resolve, reject) => {
        renderQueue.push({ renderTask, resolve, reject });
        if (!isRendering) processQueue();
    });
};
const processQueue = async () => {
    if (renderQueue.length === 0) {
        isRendering = false;
        return;
    }
    isRendering = true;
    const { renderTask, resolve, reject } = renderQueue.shift();
    try {
        const result = await renderTask();
        resolve(result);
    } catch (err) {
        reject(err);
    }
    processQueue();
};

export const RequestImageThumbnail = async (req, res) => {
    try {
        const schema = joi.object({
            assetId: joi.number().required().integer(),
            jobExpiration: joi.number().max(60).default(20).integer(),
            isFace: joi.boolean().default(false),
        });
        const { error } = schema.validate(req.body);
        if (error) {
            return responseUtil(res, 'Invalid form', 400, false, { error: error.message });
        }
        let { assetId, jobExpiration, isFace } = req.body;
        if (jobExpiration == undefined) { jobExpiration = 20; }
        if (isFace == undefined) { isFace = false; }
        const resolution = isFace ? 1680 : 600;
        const getXML = () => {
            let xml;
            if (isFace) {
                xml = JSON.parse(JSON.stringify(DecalTemplate));
                xml.Settings.Arguments[0] = `${conf.baseUrl}asset?id=${assetId}`;
                xml.Settings.Arguments[2] = resolution;
                xml.Settings.Arguments[3] = resolution;
                xml.Settings.Arguments[4] = conf.baseUrl;
            } else {
                xml = JSON.parse(JSON.stringify(ImageTemplate));
                xml.Settings.Arguments[0] = assetId;
                xml.Settings.Arguments[1] = conf.baseUrl;
                xml.Settings.Arguments[3] = resolution;
                xml.Settings.Arguments[4] = resolution;
            }
            return xml;
        };

        const xmlData = await enqueueRender(async () => {
            const response = await request({
                RCC: port,
                XML: getXML(),
                jobExpiration,
            });
            return new Promise((resolve, reject) => {
                xml2js.parseString(response.data, (err, jsXmlData) => {
                    if (err) {
                        return reject(err);
                    }
                    try {
                        const data = jsXmlData['SOAP-ENV:Envelope']['SOAP-ENV:Body'][0]['ns1:BatchJobResponse'][0]['ns1:BatchJobResult'][0]['ns1:value'][0];
                        resolve(data);
                    } catch (e) {
                        reject(e);
                    }
                });
            });
        });
        return responseUtil(res, 'success', 200, true, { data: xmlData });
    } catch (err) {
        return responseUtil(res, 'An internal server error occurred.', 500, false, { error: err.message });
    }
};

export const RequestClothingThumbnail = async (req, res) => {
    try {
        const schema = joi.object({
            assetId: joi.number().required().integer(),
            jobExpiration: joi.number().max(60).default(20).integer(),
        });
        const { error } = schema.validate(req.body);
        if (error) {
            return responseUtil(res, 'Invalid form', 400, false, { error: error.message });
        }
        let { assetId, jobExpiration } = req.body;
        if (jobExpiration == undefined) { jobExpiration = 20; }
        const assetUrl = `${conf.baseUrl}asset?id=${assetId}`;
        const xml = JSON.parse(JSON.stringify(ClothingTemplate));
        xml.Settings.Arguments[0] = assetUrl;
        xml.Settings.Arguments[2] = 1680;
        xml.Settings.Arguments[3] = 1680;
        xml.Settings.Arguments[4] = conf.baseUrl;

        const xmlData = await enqueueRender(async () => {
            const response = await request({
                RCC: port,
                XML: xml,
                jobExpiration,
            });
            return new Promise((resolve, reject) => {
                xml2js.parseString(response.data, (err, jsXmlData) => {
                    if (err) {
                        return reject(err);
                    }
                    try {
                        const data = jsXmlData['SOAP-ENV:Envelope']['SOAP-ENV:Body'][0]['ns1:BatchJobResponse'][0]['ns1:BatchJobResult'][0]['ns1:value'][0];
                        resolve(data);
                    } catch (e) {
                        reject(e);
                    }
                });
            });
        });
        return responseUtil(res, 'success', 200, true, { data: xmlData });
    } catch (err) {
        return responseUtil(res, 'An internal server error occurred.', 500, false, { error: err.message });
    }
};

export const RequestTeeShirtThumbnail = async (req, res) => {
    try {
        const schema = joi.object({
            assetId: joi.number().required().integer(),
            jobExpiration: joi.number().max(60).default(20).integer(),
        });
        const { error } = schema.validate(req.body);
        if (error) {
            return responseUtil(res, 'Invalid form', 400, false, { error: error.message });
        }
        let { assetId, jobExpiration } = req.body;
        if (jobExpiration == undefined) { jobExpiration = 20; }
        const xml = JSON.parse(JSON.stringify(ImageTemplate));
        xml.Settings.Arguments[0] = assetId;
        xml.Settings.Arguments[1] = conf.baseUrl;
        xml.Settings.Arguments[3] = 600;
        xml.Settings.Arguments[4] = 600;

        const xmlData = await enqueueRender(async () => {
            const response = await request({
                RCC: port,
                XML: xml,
                jobExpiration,
            });
            return new Promise((resolve, reject) => {
                xml2js.parseString(response.data, async (err, jsXmlData) => {
                    if (err) {
                        return reject(err);
                    }
                    try {
                        const data = jsXmlData['SOAP-ENV:Envelope']['SOAP-ENV:Body'][0]['ns1:BatchJobResponse'][0]['ns1:BatchJobResult'][0]['ns1:value'][0];
                        resolve(data);
                    } catch (e) {
                        reject(e);
                    }
                });
            });
        });

        try {
            const TeeShirtImage = fs.readFileSync('./TeeShirtTemplate.png');
            const TShirtImage = sharp(TeeShirtImage);
            const content = sharp(Buffer.from(xmlData, 'base64'));

            const metadata = await content.metadata();
            const { width, height } = metadata;
            const aspectRatio = width / height;
            let newWidth, newHeight;
            if (width > height) {
                newWidth = 250;
                newHeight = Math.round(newWidth / aspectRatio);
            } else {
                newHeight = 250;
                newWidth = Math.round(newHeight * aspectRatio);
            }
            const resizedContentImage = await content.resize(newWidth, newHeight).toBuffer();
            const TShirtFinal = await TShirtImage.composite([{ input: resizedContentImage, top: 85, left: 85 }]).png().toBuffer();
            return responseUtil(res, 'success', 200, true, { data: TShirtFinal.toString('base64') });
        } catch (err) {
            return responseUtil(res, 'TeeShirt overlay could not be applied.', 206, false, { error: err.message, data: xmlData });
        }
    } catch (err) {
        return responseUtil(res, 'An internal server error occurred.', 500, false, { error: err.message });
    }
};