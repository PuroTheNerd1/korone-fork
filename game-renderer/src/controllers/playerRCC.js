import conf from "../util/config.js";
import request from "../util/request.js";
import enums from "../util/enums.js";
import joi from "joi";
import xml2js from 'xml2js'
import responseUtil from "../util/response.js";
import AvatarTemplate from '../../scripts/Avatar.json' with { type: 'json' };
import HeadshotTemplate from '../../scripts/Closeup.json' with { type: 'json' };
const port = enums.PlayerRCC;
const locks = new Map();

const schema = joi.object({
    userId: joi.number().required().integer(),
    jobExpiration: joi.number().max(60).default(20).integer(),
})

const handleRequest = async (req, res, template, width, height) => {
    try {
        const { error } = schema.validate(req.body)
        if (error) return responseUtil(res, 'Invalid form', 400, false, { error: error.message })
        var { userId, jobExpiration } = req.body
        if (jobExpiration == undefined) { jobExpiration = 20 }
        const characterAppearanceUrl = `${conf.baseUrl}/v1.1/avatar-fetch?placeId=0&userId=${userId}`
        const xml = JSON.parse(JSON.stringify(template));
        xml.Settings.Arguments[0] = conf.baseUrl;
        xml.Settings.Arguments[1] = characterAppearanceUrl;
        xml.Settings.Arguments[3] = width;
        xml.Settings.Arguments[4] = height;
        const response = await request({
            RCC: port,
            XML: xml,
            jobExpiration,
        })
        xml2js.parseString(response.data, (err, jsXmlData) => {
            if (err) {
                throw new Error(err.message);
            }
            const xmlData = jsXmlData['SOAP-ENV:Envelope']['SOAP-ENV:Body'][0]['ns1:BatchJobResponse'][0]['ns1:BatchJobResult'][0]['ns1:value'][0];
            return responseUtil(res, 'success', 200, true, { data: xmlData });
        })
    } catch (err) {
        console.log('[error] ', err.message)
        return responseUtil(res, 'An internal server error occurred.', 500, false, { error: err.message })
    }
}

export const RequestAvatarThumbnail = async (req, res) => {
    return await handleRequest(req, res, AvatarTemplate, 840, 840)
}

export const RequestAvatarHeadshot = async (req, res) => {
    return await handleRequest(req, res, HeadshotTemplate, 720, 720)
}