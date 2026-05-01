# switch_accounts_is_user_open
Returns if a user account is open based on account ID provided.
## Arguments
| Argument | Type | Description |
| ---- | ---- | ---- |
| Account ID | Int | ID of the user account |

## Syntax
```
switch_accounts_is_user_open(id);
```

## Returns
If the account is open or not.

## Example
```gml
//check if a user is active
    var accountid;
    for (var i = 0; i > switch_accounts_get_accounts(); i++;)
    {
        if (switch_accounts_is_user_open(i))
        {
            accountid = i;
            break;
        }       
    }
    show_debug_message(switch_accounts_get_nickname() + " is open!");
```