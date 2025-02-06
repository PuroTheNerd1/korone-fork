import conf from "../util/config.js";
import request from "../util/request.js";
import enums from "../util/enums.js";
import joi from "joi";
import xml2js from 'xml2js'
import responseUtil from "../util/response.js";
import AvatarTemplate from '../../scripts/Avatar.json' with { type: 'json' };
import HeadshotTemplate from '../../scripts/Closeup.json' with { type: 'json' };
const ports = enums.PlayerRCC;
let port = ports[0];

const schema = joi.object({
    userId: joi.number().required().integer(),
    jobExpiration: joi.number().max(60).default(20).integer(),
})

const handleRequest = async (req, res, template, width, height) => {
    try {
        const { error } = schema.validate(req.body)
        if (error) {
            throw new Error("Invalid form");
        }
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
            const xmlData = jsXmlData['SOAP-ENV:Envelope']['SOAP-ENV:Body'][0]['ns1:BatchJobResponse'][0]['ns1:BatchJobResult'][0]['ns1:value'][0];
            console.log(`[info] Rendered on port ${port} successfully`);
            return responseUtil(res, 'success', 200, true, { data: xmlData });
        })
    } catch (err) {
        // We change the render port so we can retry it on the second rcc
        if (port == ports[0]) {
            console.log(`[error] Render on port ${port[0]} failed, retrying on port ${port[1]}`);
            port = ports[1];
            let result = await handleRequest(req, res, template, width, height);
            port = ports[0];
            return result;
        }
        console.log('[error] ', err)
    }
    return responseUtil(res, 'An internal server error occurred.', 500, false)
}

export const RequestAvatarThumbnail = async (req, res) => {
    return await handleRequest(req, res, AvatarTemplate, 840, 840)
}

export const RequestAvatarHeadshot = async (req, res) => {
    return await handleRequest(req, res, HeadshotTemplate, 720, 720)
}