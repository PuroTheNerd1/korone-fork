import conf from "../util/config.js";
import request from "../util/request.js";
import enums from "../util/enums.js";
import joi from "joi";
import xml2js from 'xml2js'
import responseUtil from "../util/response.js";
import AvatarTemplate from '../../scripts/Avatar.json' with { type: 'json' };
import HeadshotTemplate from '../../scripts/Closeup.json' with { type: 'json' };
const port = enums.PlayerRCC;

export const RequestAvatarThumbnail = async (req, res) => {
    try {
        const schema = joi.object({
            userId: joi.number().required().integer(),
            jobExpiration: joi.number().max(60).default(20).integer(),
        })
        const { error } = schema.validate(req.body)
        if (error) {
            return responseUtil(res, 'Invalid form', 400, false, { error: error.message })
        }

        var { userId, jobExpiration } = req.body
        if (jobExpiration == undefined) { jobExpiration = 20 }
        const characterAppearanceUrl = `${conf.baseUrl}/v1.1/avatar-fetch?placeId=0&userId=${userId}`

        const xml = JSON.parse(JSON.stringify(AvatarTemplate));
        xml.Settings.Arguments[0] = conf.baseUrl;
        xml.Settings.Arguments[1] = characterAppearanceUrl;
        xml.Settings.Arguments[3] = 840;
        xml.Settings.Arguments[4] = 840;

        const response = await request({
            RCC: port,
            XML: xml,
            jobExpiration,
        })
        xml2js.parseString(response.data, (err, jsXmlData) => {
            if (err) {
                return responseUtil(res, 'An internal server error occurred.', 500, false, { error: err.message })
            }
            const xmlData = jsXmlData['SOAP-ENV:Envelope']['SOAP-ENV:Body'][0]['ns1:BatchJobResponse'][0]['ns1:BatchJobResult'][0]['ns1:value'][0];
            return responseUtil(res, 'success', 200, true, { data: xmlData });
        })
    } catch (err) {
        return responseUtil(res, 'An internal server error occurred.', 500, false, { error: err.message })
    }
}

export const RequestAvatarHeadshot = async (req, res) => {
    try {
        const schema = joi.object({
            userId: joi.number().required().integer(),
            jobExpiration: joi.number().default(20).max(60).integer(),
        })
        const { error } = schema.validate(req.body)
        if (error) {
            return responseUtil(res, 'Invalid form', 400, false, { error: error.message })
        }

        var { userId, jobExpiration } = req.body
        if (jobExpiration == undefined) { jobExpiration = 20 }
        const characterAppearanceUrl = `${conf.baseUrl}/v1.1/avatar-fetch?placeId=0&userId=${userId}`

        const xml = JSON.parse(JSON.stringify(HeadshotTemplate));
        xml.Settings.Arguments[0] = conf.baseUrl;
        xml.Settings.Arguments[1] = characterAppearanceUrl;
        xml.Settings.Arguments[3] = 720;
        xml.Settings.Arguments[4] = 720;

        const response = await request({
            RCC: port,
            XML: xml,
            jobExpiration,
        })
        xml2js.parseString(response.data, (err, jsXmlData) => {
            if (err) {
                return responseUtil(res, 'An internal server error occurred.', 500, false, { error: err.message })
            }
            const xmlData = jsXmlData['SOAP-ENV:Envelope']['SOAP-ENV:Body'][0]['ns1:BatchJobResponse'][0]['ns1:BatchJobResult'][0]['ns1:value'][0];
            return responseUtil(res, 'success', 200, true, { data: xmlData });
        })
    } catch (err) {
        return responseUtil(res, 'An internal server error occurred.', 500, false, { error: err.message })
    }
}