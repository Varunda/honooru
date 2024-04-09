import Vue from "vue";

export module "vue" {
    export interface Vue {
        data: () => {
            appCurrentAccountID: number;
        }
    }
}
