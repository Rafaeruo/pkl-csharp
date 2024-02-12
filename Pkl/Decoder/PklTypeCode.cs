namespace Pkl.Decoding;

public enum PklTypeCode {
	CodeObject = 0x1,
	CodeMap = 0x2,
	CodeMapping = 0x3,
	CodeList = 0x4,
	CodeListing = 0x5,
	CodeSet = 0x6,
	CodeDuration = 0x7,
	CodeDataSize = 0x8,
	CodePair = 0x9,
	CodeIntSeq = 0xA,
	CodeRegex = 0xB,
	CodeClass = 0xC,
	CodeTypeAlias = 0xD,
	CodeObjectMemberProperty = 0x10,
	CodeObjectMemberEntry = 0x11,
	CodeObjectMemberElement = 0x12
}
