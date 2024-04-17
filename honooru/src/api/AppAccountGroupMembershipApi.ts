import { Loading, Loadable } from "Loading";
import ApiWrapper from "api/ApiWrapper";

export class AppAccountGroupMembership {
    public id: number = 0;
    public accountID: number = 0;
    public groupID: number = 0;
    public timestamp: Date = new Date();
    public grantedByAccountID: number = 0;
}

export class AppAccountGroupMembershipApi extends ApiWrapper<AppAccountGroupMembership> {
    private static _instance: AppAccountGroupMembershipApi = new AppAccountGroupMembershipApi();
    public static get(): AppAccountGroupMembershipApi { return AppAccountGroupMembershipApi._instance; }

    public static parse(elem: any): AppAccountGroupMembership {
        return {
            ...elem,
            timestamp: new Date(elem.timestamp)
        };
    }

    public static getByAccountID(accountID: number): Promise<Loading<AppAccountGroupMembership[]>> {
        return AppAccountGroupMembershipApi.get().readList(`/api/group-membership/account/${accountID}`, AppAccountGroupMembershipApi.parse);
    }

    public static getByGroupID(groupID: number): Promise<Loading<AppAccountGroupMembership[]>> {
        return AppAccountGroupMembershipApi.get().readList(`/api/group-membership/group/${groupID}`, AppAccountGroupMembershipApi.parse);
    }

    public static addUserToGroup(groupID: number, accountID: number): Promise<Loading<void>> {
        return AppAccountGroupMembershipApi.get().post(`/api/group-membership/${groupID}/${accountID}`);
    }

    public static removeUserFromGroup(groupID: number, accountID: Number): Promise<Loading<void>> {
        return AppAccountGroupMembershipApi.get().delete(`/api/group-membership/${groupID}/${accountID}`);
    }

}
