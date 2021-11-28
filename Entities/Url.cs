public URL{
    
    var BASE_URL ="https://uatapi.nimbbl.tech/api";
    
    var Bearer="Bearer ";
    var AUTHURL = "v2/generate-token";
	
    var ORDER_CREATE = "v2/create-order";
    var ORDER_GET = "v2/get-order";
    var ORDER_LIST = "orders/many?f=&pt=yes";
	
    var LIST_QUERYPARAM1 = "f";
    var LIST_QUERYPARAM2 = "pt";
    var NO = "no";
    var Empty = "";
	
    var USER_CREATE = "users/create";
    var USER_GET = "users/one";
    var USER_LIST = "users/many?f=&pt=yes";
	
    var Transaction_CREATE = "transactions/create";
    var Transaction_GET = "v2/fetch-transaction";
    var Transaction_LIST = "v2/order/fetch-transactions";
	
    var ACCESS_KEY = "access_key";
    var SECRET_KEY = "access_secret";
    var TOKEN = "token";
    var Bearer = "Bearer ";

    var SEGMENT = "https://api.segment.io/v1";
    var SEGMENT_IDENTIFY = "identify";
    var SEGMENT_TRACK = "track";
    var SEGMENT_PAGE = "page";
    var SEGMENT_SCREEN = "screen";
    var SEGMENT_STRING="nvilA20f1bCvxG3GAzYgD43B6gTsdwV9";
    
    var AUTH_STATUS="auth_status";
    var KIT_NAME="kit_name";
    var KIT_VERSION="kit_version";
    var INVOICE_ID="invoice_id";
    var ORDER_ID= "order_id";
    var AMOUNT="amount";
    var MERCHANT_ID="merchant_id";
    var MERCHANT_NAME="merchant_name";
	
    var CONTEXT="context";
    var EVENT="event";
    var PROPERTIES="properties";
    var TIMESTAMP="timestamp";
    var PYTHON_SDK = "PYTHON_SDK";
    var USER = "userId";
    var ORDER_CREATION_REQ = "Order Submitted";
    var ORDER_CREATED = "Order Received";
    var AUTH_REQ = "Authorization Submitted";
    var AUTH_RES = "Authorization Received";
    
    var SUCCESS="Success";
    var FAILURE="Failure";
    
    var ANNONYMOUS_ID="anonymousId";
    var TRAITS="traits";
    var Enquiry_CREATED="Enquiry Submitted";
    var Enquiry_RECIEVED="Enquiry Recieved";
    var STATUS="status";
    var Transaction_id="Transaction Id";
}